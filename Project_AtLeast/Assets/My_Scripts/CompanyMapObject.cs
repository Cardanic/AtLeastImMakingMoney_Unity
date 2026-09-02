using UnityEngine;
using TMPro;

public class CompanyMapObject : MonoBehaviour
{
    [Tooltip("Drag the 3D TextMeshPro child here")]
    public TextMeshPro nameText;

    [Tooltip("Camera tour orbit direction when viewed from above. Default clockwise.")]
    public bool orbitClockwise = true;

    public Organization BoundData { get; private set; }

    public void Bind(Organization org)
    {
        BoundData = org;
        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshPro>(true);
        if (nameText != null)
            nameText.text = org.company_name;
    }
}
