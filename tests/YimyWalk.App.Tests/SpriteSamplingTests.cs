using YimyWalk.App.Rendering;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;

namespace YimyWalk.App.Tests;

public sealed class SpriteSamplingTests
{
    [Fact]
    public void SmoothDownsamplePreservesThinCoverageWithoutTransparentColorFringes()
    {
        using var definition = Definition(SpriteSampling.Smooth);
        using var owner = new DefinitionLease(definition);
        using var cache = new FrameSurfaceCache();
        using var lease = cache.Acquire(definition, definition.Frames[0], false, .5f);
        var color = lease.Surface.Bitmap.GetPixel(0, 0);
        Assert.InRange(color.A, 1, 254);
        Assert.True(color.R > 220);
        Assert.True(color.B < 10); // transparent blue texels cannot tint red artwork
        Assert.Equal(color.A, lease.Surface.Alpha[0]);
    }

    [Fact]
    public void NativePixelsAndLegacyNearestRemainUnfiltered()
    {
        foreach (var sampling in new[] { SpriteSampling.Nearest, SpriteSampling.Smooth })
        {
            using var definition = Definition(sampling);
            using var owner = new DefinitionLease(definition);
            using var cache = new FrameSurfaceCache();
            using var native = cache.Acquire(definition, definition.Frames[0], false, 1);
            Assert.Equal(Color.FromArgb(255, 255, 0, 0), native.Surface.Bitmap.GetPixel(0, 0));
            Assert.Equal(0, native.Surface.Bitmap.GetPixel(1, 0).A);
            using var left = cache.Acquire(definition, definition.Frames[0], true, 1);
            Assert.Equal(native.Surface.Bitmap.GetPixel(0, 0), left.Surface.Bitmap.GetPixel(1, 0));
            if (sampling == SpriteSampling.Nearest)
            {
                using var small = cache.Acquire(definition, definition.Frames[0], false, .5f);
                Assert.Equal(255, small.Surface.Alpha[0]);
            }
        }
    }

    private static CharacterDefinition Definition(SpriteSampling sampling) => new()
    {
        Id = Guid.NewGuid().ToString(), DisplayName = "sampling", CellWidth = 2, CellHeight = 2,
        Scale = 1, Anchor = new(1, 2), Sampling = sampling,
        Movement = new(60, 450, 1500, 1500), Defaults = new(2, 8, 45), Dialogue = DialogueSet.Default,
        Frames = Enumerable.Range(0, 9).Select(i => new CharacterFrame(i, 2, 2,
            new byte[] { 255, 0, 0, 255, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255, 0 }, FrameKind.Idle)).ToArray()
    };
}
