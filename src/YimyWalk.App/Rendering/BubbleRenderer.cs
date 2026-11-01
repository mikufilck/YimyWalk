using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace YimyWalk.App.Rendering;

/// <summary>App-owned artwork; neither the skin nor drawing resources enter Core.</summary>
internal sealed record BubbleArtwork(Bitmap Pixels, RectangleF TextBounds, bool TextTruncated) : IDisposable
{
    public void Dispose() => Pixels.Dispose();
}

internal static class BubbleRenderer
{
    internal static BubbleArtwork Render(string text, int dpi, Size available)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(available.Width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(available.Height, 1);
        // Keep all corner ornaments at a fixed device-independent size. On an
        // unusually small work area the entire layout scales down to fit.
        var scale = Math.Min(Math.Clamp(dpi / 96f, 0.5f, 8f),
            Math.Min(available.Width / 112f, available.Height / 80f));
        using var font = new Font("Microsoft YaHei UI", Math.Max(1, 13.5f * scale), FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat(StringFormat.GenericDefault)
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.LineLimit
        };
        using var measurement = new Bitmap(1, 1);
        using var metrics = Graphics.FromImage(measurement);
        var maxTextWidth = Math.Max(1, Math.Min(280 * scale, available.Width - 50 * scale));
        var measured = metrics.MeasureString(text, font, new SizeF(maxTextWidth, float.MaxValue), format);
        var width = Math.Min(available.Width, Math.Max(1, (int)Math.Ceiling(Math.Max(112 * scale, measured.Width + 50 * scale))));
        var height = Math.Min(available.Height, Math.Max(1, (int)Math.Ceiling(measured.Height + 52 * scale)));
        var textBounds = new RectangleF(22 * scale, 30 * scale,
            Math.Max(1, width - 50 * scale), Math.Max(1, height - 52 * scale));
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            using var stream = typeof(BubbleRenderer).Assembly.GetManifestResourceStream("YimyWalk.App.Resources.Dialogue.yimy-bubble.png")
                ?? throw new InvalidOperationException("Embedded Yimy bubble skin is missing.");
            using var skin = new Bitmap(stream);
            DrawNineSlice(graphics, skin, bitmap.Size, scale);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using var ink = new SolidBrush(Color.FromArgb(104, 77, 94));
            graphics.DrawString(text, font, ink, textBounds, format);
            return new BubbleArtwork(bitmap, textBounds, measured.Height > textBounds.Height + 1);
        }
        catch { bitmap.Dispose(); throw; }
    }

    private static void DrawNineSlice(Graphics graphics, Bitmap skin, Size size, float scale)
    {
        // Audited 2048x768 skin. The cuts leave the ears, paw, bow, bell and
        // speech tail inside fixed caps; only plain fill and straight rims stretch.
        int[] sourceX = [0, 600, 1600, skin.Width];
        int[] sourceY = [0, 360, 510, skin.Height];
        var pixelScale = 0.075f * scale;
        int[] targetX = [0, (int)Math.Round(600 * pixelScale), size.Width - (int)Math.Round(448 * pixelScale), size.Width];
        int[] targetY = [0, (int)Math.Round(360 * pixelScale), size.Height - (int)Math.Round(258 * pixelScale), size.Height];
        using var attributes = new ImageAttributes();
        attributes.SetWrapMode(WrapMode.TileFlipXY);
        for (var row = 0; row < 3; row++)
        for (var column = 0; column < 3; column++)
        {
            var destination = Rectangle.FromLTRB(targetX[column], targetY[row], targetX[column + 1], targetY[row + 1]);
            if (destination.Width <= 0 || destination.Height <= 0) continue;
            graphics.DrawImage(skin, destination, sourceX[column], sourceY[row],
                sourceX[column + 1] - sourceX[column], sourceY[row + 1] - sourceY[row], GraphicsUnit.Pixel, attributes);
        }
    }

    internal static Point Place(Rectangle petBounds, Size bubbleSize, Rectangle workingArea, int dpi)
    {
        var scale = Math.Clamp(dpi / 96f, 0.5f, 8f);
        var x = petBounds.Left + petBounds.Width / 2 - (int)Math.Round(22 * scale);
        var y = petBounds.Top - bubbleSize.Height - (int)Math.Round(4 * scale);
        return new Point(Math.Clamp(x, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - bubbleSize.Width)),
            Math.Clamp(y, workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - bubbleSize.Height)));
    }
}
