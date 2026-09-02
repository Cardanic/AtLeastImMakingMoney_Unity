/// <summary>
/// Dataset exposure scores are 0–1 in the fifty-companies export and 0–100 in older files.
/// Dials stay 0–100, so values in the unit interval are scaled up on load.
/// </summary>
public static class ExposureScore
{
    public static float? ToHundred(float? score)
    {
        if (!score.HasValue)
            return null;
        return score.Value <= 1f ? score.Value * 100f : score.Value;
    }
}
