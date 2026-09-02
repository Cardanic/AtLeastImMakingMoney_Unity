using System;
using System.Globalization;

/// <summary>Compact euro strings such as <c>4M€</c>, <c>59.5B€</c>, <c>500€</c>.</summary>
public static class CompactEuroFormat
{
    public static string Format(double amount)
    {
        double magnitude = Math.Abs(amount);
        if (magnitude >= 1e9)
            return Compact(amount / 1e9, "B");
        if (magnitude >= 1e6)
            return Compact(amount / 1e6, "M");
        if (magnitude >= 1e3)
            return Compact(amount / 1e3, "K");
        return Compact(amount, string.Empty);
    }

    static string Compact(double scaled, string suffix)
    {
        return scaled.ToString("0.#", CultureInfo.InvariantCulture) + suffix + "€";
    }
}
