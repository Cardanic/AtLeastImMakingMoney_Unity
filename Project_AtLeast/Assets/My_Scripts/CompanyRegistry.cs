using System.Collections.Generic;

public static class CompanyRegistry
{
    public static Dictionary<int, CompanyCardUI> UICards = new Dictionary<int, CompanyCardUI>();
    public static Dictionary<int, CompanyMapObject> MapObjects = new Dictionary<int, CompanyMapObject>();

    public static void ClearUICards() => UICards.Clear();
    public static void ClearMapObjects() => MapObjects.Clear();
}