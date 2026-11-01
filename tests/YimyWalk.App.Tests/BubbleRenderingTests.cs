using YimyWalk.App.Rendering;

namespace YimyWalk.App.Tests;

public sealed class BubbleRenderingTests
{
    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(192)]
    public void SkinKeepsTransparentOutsideOpaqueTextAreaAndUnstretchedCorners(int dpi)
    {
        using var shortLine = BubbleRenderer.Render("我在呀！", dpi, new Size(1200, 900));
        using var longLine = BubbleRenderer.Render("今天也陪着你，汪！要一起听一首歌吗？", dpi, new Size(1200, 900));
        Assert.False(shortLine.TextTruncated);
        Assert.False(longLine.TextTruncated);
        Assert.True(longLine.Pixels.Width > shortLine.Pixels.Width);
        Assert.Equal(0, longLine.Pixels.GetPixel(0, 0).A);
        var bounds = longLine.TextBounds;
        Assert.True(longLine.Pixels.GetPixel((int)bounds.Left, (int)bounds.Top).A >= 240);
        Assert.True(bounds.Right < longLine.Pixels.Width && bounds.Bottom < longLine.Pixels.Height);
        // The top-left ornament must have identical pixels for different text widths.
        for (var y = 0; y < 20 * dpi / 96; y++)
        for (var x = 0; x < 35 * dpi / 96; x++)
            Assert.Equal(shortLine.Pixels.GetPixel(x, y), longLine.Pixels.GetPixel(x, y));
    }

    [Fact]
    public void LongDialogueWrapsAndEllipsizesWithinAvailableWorkArea()
    {
        using var shortLine = BubbleRenderer.Render("咿米在这里。", 144, new Size(600, 400));
        using var wrapped = BubbleRenderer.Render("今天也陪着你，汪！\n我们一起听一首歌吧。", 144, new Size(600, 400));
        using var bounded = BubbleRenderer.Render(new string('米', 500), 144, new Size(240, 200));
        Assert.True(wrapped.Pixels.Height > shortLine.Pixels.Height);
        Assert.False(wrapped.TextTruncated);
        Assert.InRange(bounded.Pixels.Width, 1, 240);
        Assert.InRange(bounded.Pixels.Height, 1, 200);
        Assert.True(bounded.TextTruncated);
    }

    [Fact]
    public void PlacementFitsNegativeAndOffsetWorkingAreas()
    {
        var size = new Size(260, 100);
        foreach (var area in new[] { new Rectangle(-1920, -1080, 1920, 1080), new Rectangle(200, 80, 800, 600) })
        foreach (var corner in new[] { area.Location, new Point(area.Right - 10, area.Bottom - 10) })
        {
            var point = BubbleRenderer.Place(new Rectangle(corner, new Size(80, 80)), size, area, 144);
            Assert.True(area.Contains(new Rectangle(point, size)));
        }
    }
}
