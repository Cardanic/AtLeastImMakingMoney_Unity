using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slowly ping-pongs a vertical ScrollRect when content is taller than the viewport.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class PortfolioAutoScroll : MonoBehaviour
{
    [SerializeField] float pixelsPerSecond = 28f;
    [SerializeField] float pauseAtEndsSeconds = 1.4f;

    ScrollRect _scroll;
    float _direction = -1f;
    float _pauseLeft;

    void Awake()
    {
        _scroll = GetComponent<ScrollRect>();
    }

    void OnEnable()
    {
        ResetToTop();
    }

    void LateUpdate()
    {
        if (_scroll == null || pixelsPerSecond <= 0f)
            return;

        float overflow = OverflowPixels();
        if (overflow <= 1f)
        {
            _scroll.verticalNormalizedPosition = 1f;
            _direction = -1f;
            return;
        }

        if (_pauseLeft > 0f)
        {
            _pauseLeft -= Time.unscaledDeltaTime;
            return;
        }

        float delta = (pixelsPerSecond * Time.unscaledDeltaTime) / overflow;
        float y = _scroll.verticalNormalizedPosition + _direction * delta;

        if (y <= 0f)
        {
            _scroll.verticalNormalizedPosition = 0f;
            _direction = 1f;
            _pauseLeft = pauseAtEndsSeconds;
        }
        else if (y >= 1f)
        {
            _scroll.verticalNormalizedPosition = 1f;
            _direction = -1f;
            _pauseLeft = pauseAtEndsSeconds;
        }
        else
        {
            _scroll.verticalNormalizedPosition = y;
        }
    }

    public void ResetToTop()
    {
        if (_scroll != null)
            _scroll.verticalNormalizedPosition = 1f;
        _direction = -1f;
        _pauseLeft = pauseAtEndsSeconds;
    }

    float OverflowPixels()
    {
        RectTransform content = _scroll.content;
        RectTransform viewport = _scroll.viewport != null
            ? _scroll.viewport
            : (RectTransform)_scroll.transform;
        if (content == null || viewport == null)
            return 0f;

        return content.rect.height - viewport.rect.height;
    }
}
