using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CompanyCardUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text industryText;
    public TMP_Text revenueText;
    public TMP_Text armsPctText;

    //private CompanyMetrics boundData;

    private Organization boundData;

    public void Bind(Organization org)
    {
        boundData = org;

        nameText.text = org.company_name;
        industryText.text = $"{org.industry} / {org.sub_industry}";

        double revenue2024 = org.total_revenue_2024_euro ?? 0;
        revenueText.text = $"€{revenue2024:N0} ({org.total_revenue_change})";

        double armsPct = org.arms_revenue_pct_of_total ?? 0;
        armsPctText.text = $"{armsPct:F0}% arms revenue";
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