using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Dataset column a dial reads out, or <see cref="None"/> to show the raw dial number.</summary>
public enum ParameterEuroMetric
{
    None = 0,
    LobbyingCostEU = 1,
    MilitaryRevenue2024 = 2
}

/// <summary>
/// Forces a child slider to integer steps and mirrors the value into a label.
/// Defaults to 0–100; set <see cref="maxValue"/> to 1 for binary dials.
/// With a <see cref="metric"/> assigned, the center label shows a compact euro amount
/// scaled linearly against the dataset maximum (dial 50 of 100 = half the maximum).
/// Dial at minimum shows n/d, never 0€.
/// </summary>
public sealed class IntegerSliderLabel : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI label;

    [SerializeField]
    float minValue = 0f;

    [SerializeField]
    float maxValue = 100f;

    [Header("Euro scale (optional)")]
    [SerializeField]
    ParameterEuroMetric metric = ParameterEuroMetric.None;

    [Tooltip("Shows the dataset maximum for the chosen metric.")]
    [SerializeField]
    TextMeshProUGUI maxLabel;

    [Tooltip("Shown at dial minimum, where no company has a disclosed amount.")]
    [SerializeField]
    string unavailableText = "n/d";

    [Header("Binary labels (optional)")]
    [Tooltip("Center label when the dial is at minimum (e.g. 0 on a 0–1 slider).")]
    [SerializeField]
    string labelAtMin;

    [Tooltip("Center label when the dial is at maximum (e.g. 1 on a 0–1 slider).")]
    [SerializeField]
    string labelAtMax;

    [Tooltip("Leave empty to find the filter in the scene.")]
    [SerializeField]
    MsciWorldCompanyFilter filter;

    Slider _slider;
    double _datasetMax;
    bool _scaleReady;

    void Awake()
    {
        _slider = GetComponentInChildren<Slider>(true);
        if (_slider == null)
            return;

        _slider.minValue = minValue;
        _slider.maxValue = maxValue;
        _slider.wholeNumbers = true;
        _slider.onValueChanged.AddListener(UpdateLabel);
    }

    // The filter parses its dataset in Awake, so the maximum is only readable from Start on.
    void Start()
    {
        ResolveDatasetMax();
        _scaleReady = true;
        if (_slider != null)
            UpdateLabel(_slider.value);
    }

    void OnDestroy()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(UpdateLabel);
    }

    void ResolveDatasetMax()
    {
        if (metric == ParameterEuroMetric.None)
            return;

        if (filter == null)
            filter = FindFirstObjectByType<MsciWorldCompanyFilter>();

        if (filter == null)
        {
            Debug.LogWarning(
                $"{nameof(IntegerSliderLabel)} on {name}: no {nameof(MsciWorldCompanyFilter)} in the " +
                "scene, falling back to the raw dial number.");
            return;
        }

        _datasetMax = metric == ParameterEuroMetric.LobbyingCostEU
            ? filter.MaxLobbyingCostEU
            : filter.MaxMilitaryRevenue2024Euro;

        if (maxLabel != null && _datasetMax > 0.0)
            maxLabel.text = CompactEuroFormat.Format(_datasetMax, CurrencySymbol);
    }

    void UpdateLabel(float value)
    {
        if (label == null)
            return;
        if (metric != ParameterEuroMetric.None && !_scaleReady)
            return;

        if (TryBinaryLabel(value, out string binaryText))
        {
            label.text = binaryText;
            return;
        }

        label.text = EuroDialReadout.CenterLabel(
            value, minValue, maxValue, _datasetMax, unavailableText, CurrencySymbol);
    }

    bool TryBinaryLabel(float value, out string text)
    {
        text = null;
        if (string.IsNullOrEmpty(labelAtMin) || string.IsNullOrEmpty(labelAtMax))
            return false;
        if (!Mathf.Approximately(minValue, 0f) || !Mathf.Approximately(maxValue, 1f))
            return false;

        text = Mathf.RoundToInt(value) >= 1 ? labelAtMax : labelAtMin;
        return true;
    }

    string CurrencySymbol =>
        metric == ParameterEuroMetric.MilitaryRevenue2024 ? "$" : "€";
}
