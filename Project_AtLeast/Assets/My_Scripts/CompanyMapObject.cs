using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CompanyMapObject : MonoBehaviour
{
    [Tooltip("Drag the 3D TextMeshPro child here")]
    public TextMeshPro nameText;

    [Tooltip("Camera tour orbit direction when viewed from above. Default clockwise.")]
    public bool orbitClockwise = true;

    public Organization BoundData { get; private set; }

    /// <summary>Company display name shown by the detection HUD.</summary>
    public string DisplayName =>
        BoundData != null && !string.IsNullOrEmpty(BoundData.company_name)
            ? BoundData.company_name
            : name;

    /// <summary>Readout rows for the detection HUD. Populated from the data layer.</summary>
    public List<DetectionField> DetectionFields { get; } = new List<DetectionField>();

    public void Bind(Organization org)
    {
        BoundData = org;
        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshPro>(true);
        if (nameText != null)
            nameText.text = org.company_name;
    }
}
