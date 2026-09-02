using System;
using System.Collections.Generic;

/// <summary>
/// Maps a 0–100 (or other) dial onto a dataset euro maximum for display.
/// Dial at minimum is <c>n/d</c>, not 0€. Filtering still uses the raw dial score.
/// </summary>
public static class EuroDialReadout
{
    public static string CenterLabel(
        float dialValue,
        float minValue,
        float maxValue,
        double datasetMax,
        string unavailableText = "n/d")
    {
        if (datasetMax <= 0.0)
            return Math.Round(dialValue).ToString("0");

        if (dialValue <= minValue)
            return unavailableText;

        double span = maxValue - minValue;
        double fraction = span > 0.0 ? (dialValue - minValue) / span : 0.0;
        return CompactEuroFormat.Format(fraction * datasetMax);
    }

    public static double MaxOf(IReadOnlyList<Organization> companies, Func<Organization, double?> selector)
    {
        double max = 0.0;
        if (companies == null)
            return max;

        for (int i = 0; i < companies.Count; i++)
        {
            double? value = selector(companies[i]);
            if (value.HasValue && value.Value > max)
                max = value.Value;
        }

        return max;
    }
}
