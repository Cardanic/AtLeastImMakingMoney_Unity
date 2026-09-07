using System;

/// <summary>
/// One readout row for the machine-vision HUD. Populated from the data layer.
/// </summary>
[Serializable]
public class DetectionField
{
    public string key;
    public string value;
    public float confidence;
    public bool isDisclosed;
}
