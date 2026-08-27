using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Forces a child slider to integer steps 0–100 and mirrors the value into a label.
/// </summary>
public sealed class IntegerSliderLabel : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI label;

    Slider _slider;

    void Awake()
    {
        _slider = GetComponentInChildren<Slider>(true);
        if (_slider == null)
            return;

        _slider.minValue = 0f;
        _slider.maxValue = 100f;
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
