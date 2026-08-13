using UnityEngine;
using TMPro;

public class CompanyMapObject : MonoBehaviour
{
    [Tooltip("Drag the 3D TextMeshPro child here")]
    public TextMeshPro nameText;

    public MsciWorldCompanyFilter.Organization BoundData { get; private set; }

    public void Bind(MsciWorldCompanyFilter.Organization org)
    {
        BoundData = org;
        if (nameText != null)
            nameText.text = org.company_name;
    }
}