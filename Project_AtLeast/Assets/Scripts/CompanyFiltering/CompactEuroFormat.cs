using System;
using System.Globalization;

/// <summary>Compact money strings such as <c>4M€</c>, <c>64.7B$</c>, <c>500€</c>.</summary>
public static class CompactEuroFormat
{
    public static string Format(double amount, string currencySymbol = "€")
    {
        double magnitude = Math.Abs(amount);
        if (magnitude >= 1e9)
            return Compact(amount / 1e9, "B", currencySymbol);
        if (magnitude >= 1e6)
            return Compact(amount / 1e6, "M", currencySymbol);
        if (magnitude >= 1e3)
            return Compact(amount / 1e3, "K", currencySymbol);
        return Compact(amount, string.Empty, currencySymbol);
    }

    static string Compact(double scaled, string suffix, string currencySymbol)
    {
        return scaled.ToString("0.#", CultureInfo.InvariantCulture) + suffix + currencySymbol;
    }
}
