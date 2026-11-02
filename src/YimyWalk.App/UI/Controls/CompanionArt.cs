using System.Drawing.Drawing2D;

namespace YimyWalk.App.UI.Controls;

internal sealed class CompanionArt : Control
{
    private readonly Bitmap _portrait;
    public CompanionArt()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent; TabStop = false;
        using var stream = typeof(CompanionArt).Assembly.GetManifestResourceStream("YimyWalk.App.Resources.icon_yimy.png") ?? throw new InvalidOperationException("Yimy portrait is missing.");
        using var source = Image.FromStream(stream); _portrait = new Bitmap(source);
        AccessibleName = "咿米的原画装饰";
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var size = Math.Min(Width, Height); var x = (Width - size) / 2f; var y = (Height - size) / 2f;
        using var halo = new SolidBrush(Color.FromArgb(255, 231, 241));
        g.FillEllipse(halo, x + size * .07f, y + size * .08f, size * .86f, size * .86f);
        using var ring = new Pen(Color.FromArgb(231, 182, 205), 1);
        ring.DashPattern = [2, 5]; g.DrawEllipse(ring, x + size * .03f, y + size * .04f, size * .94f, size * .94f);
        UiMotifs.Bone(g, new RectangleF(x, y + size * .1f, size * .18f, size * .09f), Color.White, UiTheme.Border);
        UiMotifs.Paw(g, new RectangleF(x + size * .85f, y + size * .78f, size * .13f, size * .13f), Color.FromArgb(225, 167, 191));
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(_portrait, new RectangleF(x + size * .06f, y + size * .02f, size * .88f, size * .88f));
    }
    protected override void Dispose(bool disposing) { if (disposing) _portrait.Dispose(); base.Dispose(disposing); }
}
