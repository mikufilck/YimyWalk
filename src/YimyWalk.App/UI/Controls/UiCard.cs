using System.Drawing.Drawing2D;

namespace YimyWalk.App.UI.Controls;

internal sealed class UiCard : Panel
{
    public Color SurfaceColor { get; set; } = Color.White;
    public bool DecorativeEdge { get; set; }
    public UiCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UiMotifs.Round(new RectangleF(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 4)), 20 * DeviceDpi / 96f);
        using var fill = new SolidBrush(SurfaceColor); using var border = new Pen(UiTheme.Border);
        e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(border, path);
        if (DecorativeEdge)
        {
            var scale = DeviceDpi / 96f;
            using var stitch = new Pen(UiTheme.Coral, scale) { DashStyle = DashStyle.Dot, DashCap = DashCap.Round };
            e.Graphics.DrawLine(stitch, 28 * scale, Height - 15 * scale, Width - 28 * scale, Height - 15 * scale);
        }
        base.OnPaint(e);
    }
}
