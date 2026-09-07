using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Machine-vision style HUD. Builds a TargetFrame (four corner brackets + label) and a
/// stack of field boxes at runtime under this Canvas, then per frame "detects" the building
/// <see cref="CameraFocus.CurrentTarget"/> and reads out its <see cref="CompanyMapObject.DetectionFields"/>.
///
/// Attach to a Screen Space Overlay Canvas. Everything is one accent colour, one monospace font.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Canvas))]
public class DetectionHud : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("The single accent colour used for every HUD element.")]
    public Color accent = new Color(1f, 0.16470589f, 0.10196079f, 1f); // #FF2A1A

    [Tooltip("Monospace font asset for all HUD text.")]
    public TMP_FontAsset monoFont;

    public float labelFontSize = 14f;
    public float fieldFontSize = 12f;

    [Header("Target frame")]
    [Tooltip("Camera used to aim the inset (E). Falls back to the tour camera, then Camera.main.")]
    public Camera projectionCamera;

    [Tooltip("Fixed panel size, in pixels. The panel no longer tracks the building on screen.")]
    public Vector2 frameSize = new Vector2(240f, 150f);

    [Tooltip("Anchored offset from the screen's top-right corner, in pixels (negative = inward).")]
    public Vector2 frameScreenOffset = new Vector2(-24f, -24f);

    [Tooltip("Length of each corner bracket arm, in pixels.")]
    public float bracketArmLength = 22f;

    [Tooltip("Thickness of every 1px line in the HUD, in pixels.")]
    public float lineThickness = 1f;

    [Tooltip("Rect scale at TargetLock 0.")]
    public float frameScaleUnlocked = 1.4f;

    [Tooltip("Rect scale at TargetLock 1.")]
    public float frameScaleLocked = 1f;

    [Tooltip("Label position relative to the frame's top-left corner, in pixels.")]
    public Vector2 labelOffset = new Vector2(0f, 6f);

    [Header("Scan reveal (C)")]
    [Tooltip("Thickness of the bright scan-front line, in pixels.")]
    public float scanBandThicknessPx = 3f;

    [Header("Lock-in glitch")]
    [Tooltip("How long the frame jitters after TargetLock crosses 0.5.")]
    public float lockGlitchDuration = 0.25f;

    [Tooltip("Peak rect jitter during the lock-in glitch, in pixels.")]
    public float lockJitterPx = 6f;

    [Tooltip("Seconds between jitter samples during the lock-in glitch.")]
    public float lockJitterInterval = 0.04f;

    [Tooltip("Peak alpha flicker (+/-) during the lock-in glitch.")]
    public float lockAlphaFlicker = 0.3f;

    [Header("Field boxes")]
    [Min(0)]
    public int fieldBoxCount = 4;

    public Vector2 fieldBoxSize = new Vector2(190f, 22f);

    [Tooltip("Offset of the first box from the frame's bottom-right corner, in pixels.")]
    public Vector2 fieldBoxOffset = new Vector2(0f, -16f);

    [Tooltip("Added position per box, stacking the list. In pixels.")]
    public Vector2 fieldBoxStride = new Vector2(0f, -28f);

    [Tooltip("Seconds between each box appearing after the frame locks.")]
    public float fieldBoxStagger = 0.12f;

    [Tooltip("Horizontal text inset inside a field box, in pixels.")]
    public float fieldBoxTextPadding = 6f;

    [Header("Undisclosed fields")]
    [Tooltip("Peak jitter (+/-) for an undisclosed box, in pixels.")]
    public float undisclosedJitterPx = 3f;

    [Tooltip("Seconds between jitter samples for an undisclosed box.")]
    public float undisclosedJitterInterval = 0.06f;

    [Range(0f, 1f)]
    [Tooltip("Alpha ceiling for an undisclosed box.")]
    public float undisclosedAlphaCap = 0.6f;

    [Header("Camera feed (E) — rendered inside the target frame")]
    public bool enableInset = true;

    [Tooltip("Custom/InsetTear. Left empty, it is found by name.")]
    public Shader insetShader;

    public float insetFieldOfView = 12f;

    [Min(16)]
    public int insetResolution = 512;

    [Range(0f, 1f)]
    [Tooltip("TargetLock above which the camera starts rendering into the panel.")]
    public float insetShowLock = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("TargetLock below which the camera hard-cuts off (stops rendering).")]
    public float insetHideLock = 0.3f;

    [Tooltip("Layers the feed camera renders. The UI layer is always excluded.")]
    public LayerMask insetCullingMask = -1;

    Canvas _canvas;
    RectTransform _canvasRect;
    Camera _fallbackCam;

    RectTransform _frame;
    CanvasGroup _frameGroup;
    TextMeshProUGUI _label;

    RectTransform _scanBand;

    readonly List<FieldBox> _boxes = new();

    readonly StringBuilder _sb = new(96);

    float _prevLock;
    float _glitchEndTime = -1f;
    float _nextGlitchTime;
    Vector2 _glitchOffset;
    float _glitchAlphaDelta;
    float _lockTime = -1f;
    CompanyMapObject _shownTarget;

    RectTransform _feedMaskRoot;
    CanvasGroup _feedGroup;
    RawImage _feedImage;
    Camera _insetCam;
    RenderTexture _insetRT;
    Material _insetMaterial;
    bool _insetWantsShow;

    static readonly int HudTearRowsId = Shader.PropertyToID("_HudTearRows");
    static readonly int HudTearIntervalId = Shader.PropertyToID("_HudTearInterval");
    static readonly int HudTearAmountId = Shader.PropertyToID("_HudTearAmount");
    static readonly int HudTearCountId = Shader.PropertyToID("_HudTearCount");
    static readonly int HudTearIntensityId = Shader.PropertyToID("_HudTearIntensity");

    class FieldBox
    {
        public RectTransform rect;
        public CanvasGroup group;
        public TextMeshProUGUI text;
        public float nextJitterTime;
        public Vector2 jitterOffset;

        public string cachedKey;
        public string cachedValue;
        public float cachedConfidence = float.NaN;
        public bool cachedDisclosed;
        public bool cachedValid;
    }

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvasRect = (RectTransform)transform;

        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            Debug.LogWarning("DetectionHud expects a Screen Space Overlay Canvas.", this);

        BuildFrame();
        BuildBoxes();
        HideAll();
    }

    void OnDestroy()
    {
        if (_insetCam != null)
            Destroy(_insetCam.gameObject);
        if (_insetRT != null)
        {
            _insetRT.Release();
            Destroy(_insetRT);
        }
        if (_insetMaterial != null)
            Destroy(_insetMaterial);
    }

    void LateUpdate()
    {
        CameraFocus cf = CameraFocus.Instance;
        Camera cam = ResolveCamera(cf);

        PushTearGlobals(cf);

        CompanyMapObject target = cf != null ? cf.CurrentTarget : null;

        if (cf == null || target == null)
        {
            HideAll();
            _prevLock = 0f;
            _lockTime = -1f;
            _shownTarget = null;
            return;
        }

        float lock01 = Mathf.Clamp01(cf.TargetLock);

        if (target != _shownTarget)
        {
            _shownTarget = target;
            _lockTime = -1f;
            _glitchEndTime = -1f;
            InvalidateBoxCaches();
        }

        // Lock-in event: TargetLock crossed 0.5 upward.
        if (_prevLock < 0.5f && lock01 >= 0.5f)
        {
            _glitchEndTime = Time.time + lockGlitchDuration;
            _nextGlitchTime = 0f;
            if (_lockTime < 0f)
                _lockTime = Time.time;
        }
        _prevLock = lock01;

        bool frameVisible = UpdatePanel(cf, cam, lock01);
        UpdateBoxes(cf, lock01, frameVisible);
    }

    Camera ResolveCamera(CameraFocus cf)
    {
        if (projectionCamera != null)
            return projectionCamera;
        if (_fallbackCam == null && cf != null)
            _fallbackCam = cf.GetComponent<Camera>();
        if (_fallbackCam != null)
            return _fallbackCam;
        return Camera.main;
    }

    // ---- Per-frame: target panel (fixed top-right; glitch + scan + tear live only here) -----

    bool UpdatePanel(CameraFocus cf, Camera cam, float lock01)
    {
        // Lock-in glitch: whole-panel jitter + alpha flicker for a short window after lock.
        Vector2 glitchOffset = Vector2.zero;
        float alphaDelta = 0f;
        if (Time.time < _glitchEndTime)
        {
            if (Time.time >= _nextGlitchTime)
            {
                _nextGlitchTime = Time.time + Mathf.Max(0.0001f, lockJitterInterval);
                _glitchOffset = new Vector2(
                    UnityEngine.Random.Range(-lockJitterPx, lockJitterPx),
                    UnityEngine.Random.Range(-lockJitterPx, lockJitterPx));
                _glitchAlphaDelta = UnityEngine.Random.Range(-lockAlphaFlicker, lockAlphaFlicker);
            }
            glitchOffset = _glitchOffset;
            alphaDelta = _glitchAlphaDelta;
        }
        else
        {
            _glitchOffset = Vector2.zero;
            _glitchAlphaDelta = 0f;
        }

        SetFrameVisible(true);
        _frame.anchoredPosition = frameScreenOffset + glitchOffset;
        _frame.localScale = Vector3.one * Mathf.Lerp(frameScaleUnlocked, frameScaleLocked, lock01);
        _frameGroup.alpha = Mathf.Clamp01(lock01 + alphaDelta);

        _label.text = _shownTarget.DisplayName;

        UpdateFeedCamera(cf, cam, lock01);
        UpdateScanReveal(lock01);

        return true;
    }

    // E, merged into the panel: the feed camera only renders (and the RawImage only shows)
    // once TargetLock crosses insetShowLock, hard-cutting off below insetHideLock. D's tear
    // is applied by the InsetTear material itself (via the _HudTear* globals), not here.
    void UpdateFeedCamera(CameraFocus cf, Camera cam, float lock01)
    {
        if (!enableInset || _insetCam == null)
            return;

        if (lock01 >= insetShowLock) _insetWantsShow = true;
        if (lock01 < insetHideLock) _insetWantsShow = false;

        bool show = _insetWantsShow && cf.CurrentTarget != null;

        _insetCam.enabled = show;
        if (_feedGroup != null)
            _feedGroup.alpha = show ? 1f : 0f;

        if (!show)
            return;

        Vector3 center = cf.CurrentTargetBounds.center;
        Vector3 from = cam != null ? cam.transform.position : _insetCam.transform.position;
        Vector3 dir = center - from;
        if (dir.sqrMagnitude > 1e-6f)
            _insetCam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(dir.normalized, Vector3.up));
        _insetCam.fieldOfView = insetFieldOfView;
    }

    // C, on the panel: the feed mask reveals bottom-to-top as TargetLock rises, with a bright
    // scan-front line riding the edge.
    void UpdateScanReveal(float lock01)
    {
        float front = Mathf.Clamp01(lock01);

        if (_feedMaskRoot != null)
            _feedMaskRoot.sizeDelta = new Vector2(0f, front * frameSize.y);

        bool bandVisible = front > 0f && front < 1f;
        SetActive(_scanBand.gameObject, bandVisible);
        if (bandVisible)
        {
            Vector2 p = _scanBand.anchoredPosition;
            p.y = front * frameSize.y;
            _scanBand.anchoredPosition = p;
        }
    }

    // ---- Per-frame: field boxes -----------------------------------------------

    void UpdateBoxes(CameraFocus cf, float lock01, bool frameVisible)
    {
        List<DetectionField> fields = _shownTarget.DetectionFields;

        // Panel's bottom-right corner, in the same top-right-anchored space the boxes use —
        // fixed, so boxes don't slide around with the panel's lock-in jitter.
        Vector2 anchor = frameScreenOffset + new Vector2(0f, -frameSize.y);

        for (int i = 0; i < _boxes.Count; i++)
        {
            FieldBox box = _boxes[i];

            bool hasField = fields != null && i < fields.Count && fields[i] != null;
            bool revealed = _lockTime >= 0f && Time.time >= _lockTime + i * fieldBoxStagger;

            if (!frameVisible || !hasField || !revealed)
            {
                SetActive(box.rect.gameObject, false);
                continue;
            }

            SetActive(box.rect.gameObject, true);
            DetectionField f = fields[i];

            Vector2 pos = anchor + fieldBoxOffset + new Vector2(fieldBoxStride.x * i, fieldBoxStride.y * i);

            float alpha = lock01;
            Vector2 jitter = Vector2.zero;

            if (!f.isDisclosed)
            {
                alpha = Mathf.Min(alpha, undisclosedAlphaCap);
                if (Time.time >= box.nextJitterTime)
                {
                    box.nextJitterTime = Time.time + Mathf.Max(0.0001f, undisclosedJitterInterval);
                    box.jitterOffset = new Vector2(
                        UnityEngine.Random.Range(-undisclosedJitterPx, undisclosedJitterPx),
                        UnityEngine.Random.Range(-undisclosedJitterPx, undisclosedJitterPx));
                }
                jitter = box.jitterOffset;
            }
            else
            {
                box.jitterOffset = Vector2.zero;
            }

            box.rect.anchoredPosition = pos + jitter;
            box.rect.sizeDelta = fieldBoxSize;
            box.group.alpha = alpha;

            WriteBoxText(box, f);
        }
    }

    void WriteBoxText(FieldBox box, DetectionField f)
    {
        string key = f.key ?? string.Empty;
        string value = f.isDisclosed ? (f.value ?? string.Empty) : "n/d";
        float confidence = f.isDisclosed ? f.confidence : 0f;

        if (box.cachedValid
            && box.cachedKey == key
            && box.cachedValue == value
            && box.cachedDisclosed == f.isDisclosed
            && Mathf.Approximately(box.cachedConfidence, confidence))
            return;

        _sb.Clear();
        _sb.Append(key.ToUpperInvariant());
        _sb.Append("  ");
        _sb.Append(value);
        _sb.Append("  ");
        _sb.Append(confidence.ToString("F2"));
        box.text.SetText(_sb);

        box.cachedKey = key;
        box.cachedValue = value;
        box.cachedConfidence = confidence;
        box.cachedDisclosed = f.isDisclosed;
        box.cachedValid = true;
    }

    // ---- Build -----------------------------------------------------------------

    void BuildFrame()
    {
        _frame = NewChild("TargetFrame", _canvasRect);
        _frame.anchorMin = _frame.anchorMax = new Vector2(1f, 1f); // fixed, top-right of the screen
        _frame.pivot = new Vector2(1f, 1f);
        _frame.sizeDelta = frameSize;
        _frame.anchoredPosition = frameScreenOffset;

        _frameGroup = _frame.gameObject.AddComponent<CanvasGroup>();
        _frameGroup.interactable = false;
        _frameGroup.blocksRaycasts = false;

        BuildFeed();
        BuildScanBand();

        // corner index: bit 0 = right edge, bit 1 = top edge
        for (int i = 0; i < 4; i++)
        {
            bool right = (i & 1) != 0;
            bool top = (i & 2) != 0;
            Vector2 pivot = new Vector2(right ? 1f : 0f, top ? 1f : 0f);

            RectTransform corner = NewChild("Corner" + i, _frame);
            corner.anchorMin = corner.anchorMax = pivot;
            corner.pivot = pivot;
            corner.sizeDelta = Vector2.zero;
            corner.anchoredPosition = Vector2.zero;

            Image armH = NewImage("ArmH", corner);
            armH.rectTransform.anchorMin = armH.rectTransform.anchorMax = pivot;
            armH.rectTransform.pivot = pivot;
            armH.rectTransform.sizeDelta = new Vector2(bracketArmLength, lineThickness);
            armH.rectTransform.anchoredPosition = Vector2.zero;

            Image armV = NewImage("ArmV", corner);
            armV.rectTransform.anchorMin = armV.rectTransform.anchorMax = pivot;
            armV.rectTransform.pivot = pivot;
            armV.rectTransform.sizeDelta = new Vector2(lineThickness, bracketArmLength);
            armV.rectTransform.anchoredPosition = Vector2.zero;
        }

        _label = NewText("Label", _frame, labelFontSize);
        _label.alignment = TextAlignmentOptions.BottomLeft;
        RectTransform lr = _label.rectTransform;
        lr.anchorMin = lr.anchorMax = new Vector2(0f, 1f);
        lr.pivot = new Vector2(0f, 0f);
        lr.sizeDelta = new Vector2(512f, labelFontSize + 6f);
        lr.anchoredPosition = labelOffset;
    }

    // E, merged here: a masked RawImage showing the feed camera's render, revealed bottom-to-top
    // by C (mask height animates with TargetLock) and torn by D (the InsetTear material itself,
    // driven by the _HudTear* globals PushTearGlobals sets every frame).
    void BuildFeed()
    {
        _feedMaskRoot = NewChild("FeedMask", _frame);
        _feedMaskRoot.anchorMin = new Vector2(0f, 0f);
        _feedMaskRoot.anchorMax = new Vector2(1f, 0f);
        _feedMaskRoot.pivot = new Vector2(0.5f, 0f);
        _feedMaskRoot.sizeDelta = Vector2.zero; // grows in Y via UpdateScanReveal

        _feedMaskRoot.gameObject.AddComponent<RectMask2D>();
        _feedGroup = _feedMaskRoot.gameObject.AddComponent<CanvasGroup>();
        _feedGroup.interactable = false;
        _feedGroup.blocksRaycasts = false;

        if (!enableInset)
            return;

        BuildFeedCamera();

        RectTransform imgRt = NewChild("Feed", _feedMaskRoot);
        imgRt.anchorMin = new Vector2(0f, 0f);
        imgRt.anchorMax = new Vector2(1f, 0f);
        imgRt.pivot = new Vector2(0.5f, 0f);
        imgRt.sizeDelta = new Vector2(0f, frameSize.y); // full panel height; the mask clips it
        imgRt.anchoredPosition = Vector2.zero;

        _feedImage = imgRt.gameObject.AddComponent<RawImage>();
        _feedImage.raycastTarget = false;
        _feedImage.texture = _insetRT;

        Shader s = insetShader != null ? insetShader : Shader.Find("Custom/InsetTear");
        if (s != null)
        {
            _insetMaterial = new Material(s) { name = "InsetTear (runtime)" };
            _feedImage.material = _insetMaterial;
        }
    }

    void BuildFeedCamera()
    {
        var camGo = new GameObject("DetectionInsetCamera");
        _insetCam = camGo.AddComponent<Camera>();
        _insetCam.clearFlags = CameraClearFlags.SolidColor;
        _insetCam.backgroundColor = Color.black;
        _insetCam.cullingMask = insetCullingMask.value & ~(1 << 5); // never the UI layer
        _insetCam.fieldOfView = insetFieldOfView;
        _insetCam.nearClipPlane = 0.1f;
        _insetCam.farClipPlane = 3000f;
        _insetCam.enabled = false;

        int res = Mathf.Max(16, insetResolution);
        _insetRT = new RenderTexture(res, res, 24) { name = "DetectionInsetRT" };
        _insetRT.Create();
        _insetCam.targetTexture = _insetRT;
    }

    void BuildScanBand()
    {
        RectTransform rt = NewChild("ScanBand", _frame);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, scanBandThicknessPx);
        rt.anchoredPosition = Vector2.zero;

        Image img = rt.gameObject.AddComponent<Image>();
        img.color = accent;
        img.raycastTarget = false;

        _scanBand = rt;
    }

    void BuildBoxes()
    {
        for (int i = 0; i < fieldBoxCount; i++)
        {
            RectTransform rt = NewChild("FieldBox" + i, _canvasRect);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); // same fixed corner as the panel
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = fieldBoxSize;

            CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            BuildBorder(rt);

            TextMeshProUGUI txt = NewText("Text", rt, fieldFontSize);
            txt.alignment = TextAlignmentOptions.Left;
            RectTransform tr = txt.rectTransform;
            tr.anchorMin = new Vector2(0f, 0f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = new Vector2(fieldBoxTextPadding, 0f);
            tr.offsetMax = new Vector2(-fieldBoxTextPadding, 0f);

            _boxes.Add(new FieldBox { rect = rt, group = group, text = txt });
        }
    }

    void BuildBorder(RectTransform box)
    {
        BuildBorder(box, lineThickness);
    }

    void BuildBorder(RectTransform box, float thickness)
    {
        // bottom, top, left, right
        AddEdge(box, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness));
        AddEdge(box, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness));
        AddEdge(box, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f));
        AddEdge(box, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(thickness, 0f));
    }

    void AddEdge(RectTransform box, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        Image edge = NewImage("Edge", box);
        RectTransform rt = edge.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
    }

    RectTransform NewChild(string childName, RectTransform parent)
    {
        var go = new GameObject(childName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    Image NewImage(string childName, RectTransform parent)
    {
        RectTransform rt = NewChild(childName, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = accent;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI NewText(string childName, RectTransform parent, float size)
    {
        RectTransform rt = NewChild(childName, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (monoFont != null)
            t.font = monoFont;
        t.fontSize = size;
        t.color = accent;
        t.raycastTarget = false;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    // ---- Helpers -------------------------------------------------------------

    void InvalidateBoxCaches()
    {
        for (int i = 0; i < _boxes.Count; i++)
            _boxes[i].cachedValid = false;
    }

    void HideAll()
    {
        HideFrameAndBoxes();
        if (_insetCam != null)
            _insetCam.enabled = false;
        if (_feedGroup != null)
            _feedGroup.alpha = 0f;
        _insetWantsShow = false;
    }

    void HideFrameAndBoxes()
    {
        if (_frame != null)
            SetActive(_frame.gameObject, false);
        for (int i = 0; i < _boxes.Count; i++)
            SetActive(_boxes[i].rect.gameObject, false);
    }

    void PushTearGlobals(CameraFocus cf)
    {
        if (cf == null)
            return;
        Shader.SetGlobalFloat(HudTearRowsId, cf.TearRows);
        Shader.SetGlobalFloat(HudTearIntervalId, cf.TearInterval);
        Shader.SetGlobalFloat(HudTearAmountId, cf.TearAmount);
        Shader.SetGlobalFloat(HudTearCountId, cf.TearBandsPerInterval);
        Shader.SetGlobalFloat(HudTearIntensityId, cf.TearIntensity);
    }

    void SetFrameVisible(bool visible)
    {
        SetActive(_frame.gameObject, visible);
    }

    static void SetActive(GameObject go, bool active)
    {
        if (go.activeSelf != active)
            go.SetActive(active);
    }
}
