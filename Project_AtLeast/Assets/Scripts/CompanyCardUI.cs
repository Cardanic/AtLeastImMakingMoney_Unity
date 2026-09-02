using UnityEngine;
using TMPro;

public class CompanyCardUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text industryText;
    public TMP_Text revenueText;
    public TMP_Text armsPctText;

    private Organization boundData;

    public void Bind(Organization org)
    {
        boundData = org;

        nameText.text = org.company_name;
        industryText.text = CompanyCardCopy.SubIndustry(org);
        revenueText.text = CompanyCardCopy.Metrics(org);
        if (armsPctText != null)
            armsPctText.text = string.Empty;
    }

    public void OnCardClicked()
    {
        Debug.Log($"Card clicked: (id {boundData.id})");

        if (CompanyRegistry.MapObjects.TryGetValue(boundData.id, out CompanyMapObject mapObj))
        {
            Debug.Log($"Found map object at {mapObj.transform.position}");
            CameraFocus.Instance.FocusOn(mapObj.transform.position);
        }
        else
        {
            Debug.LogWarning($"No map object found for id {boundData.id}");
        }
    }
}
