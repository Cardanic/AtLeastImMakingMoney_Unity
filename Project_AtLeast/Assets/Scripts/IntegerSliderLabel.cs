using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Forces a child slider to integer steps and mirrors the value into a label.
/// Defaults to 0–100; set <see cref="maxValue"/> to 1 for binary dials.
/// </summary>
public sealed class IntegerSliderLabel : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI label;

    [SerializeField]
    float minValue = 0f;

    [SerializeField]
    float maxValue = 100f;

    Slider _slider;

    void Awake()
    {
        _slider = GetComponentInChildren<Slider>(true);
        if (_slider == null)
            return;

        _slider.minValue = minValue;
        _slider.maxValue = maxValue;
        _slider.wholeNumbers = true;
        _slider.onValueChanged.AddListener(UpdateLabel);
        UpdateLabel(_slider.value);
    }

    void OnDestroy()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(UpdateLabel);
    }

    void UpdateLabel(float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(value).ToString();
    }
}
