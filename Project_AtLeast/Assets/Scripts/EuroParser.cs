using System.Globalization;
using System.Text.RegularExpressions;

public static class EuroParser
{
    public static float ParseEuroAmount(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return 0f;
        string clean = raw.Replace("€", "").Replace(".", "").Replace("+", "").Trim();
        float.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out float result);
        return result;
    }

    public static float ParsePercent(string raw, bool normalize = false)
    {
        if (string.IsNullOrEmpty(raw)) return 0f;
        string clean = raw.Replace("%", "").Replace("+", "").Trim();
        float.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out float result);
        return normalize ? result / 100f : result;
    }

    public static (float absoluteChange, float percentChange) ParseChangeString(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return (0f, 0f);
        var match = Regex.Match(raw, @"([+-]?[\d.]+)€\s*\(([+-]?[\d.]+)%\)");
        if (!match.Success) return (0f, 0f);
        float abs = ParseEuroAmount(match.Groups[1].Value + "€");
        float pct = ParsePercent(match.Groups[2].Value + "%");
        return (abs, pct);
    }
}