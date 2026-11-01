namespace YimyWalk.Core.Simulation;

public static class PetLimits
{
    public const float MaxWalkSpeed = 2000f;
    public const int MaxRenderedDimension = 1024;
    public const long MaxRenderedPixels = 1024L * 1024;

    public static float EffectiveScale(int width, int height, float requested)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        var scale = float.IsFinite(requested) ? Math.Clamp(requested, 0.1f, 64f) : 1f;
        var limit = Math.Min(MaxRenderedDimension / (double)Math.Max(width, height),
            Math.Sqrt(MaxRenderedPixels / ((double)width * height)));
        return (float)Math.Min(scale, limit);
    }
}
