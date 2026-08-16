using System.Collections.Generic;

public static class CompanyRegistry
{
    public static readonly Dictionary<int, CompanyCardUI> UICards = new();
    public static readonly Dictionary<int, CompanyMapObject> MapObjects = new();

    public static void RegisterCard(int id, CompanyCardUI card) => UICards[id] = card;

    public static void UnregisterCard(int id) => UICards.Remove(id);

    public static void RegisterMapObject(int id, CompanyMapObject mapObject) => MapObjects[id] = mapObject;

    public static void UnregisterMapObject(int id) => MapObjects.Remove(id);
}
