using System.Drawing.Drawing2D;

namespace YimyWalk.App.UI.Controls;

internal sealed class UiCard : Panel
{
    public Color SurfaceColor { get; set; } = Color.White;
    public bool LaceEdge { get; set; }
    public string? Motif { get; set; }
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
        if (Motif == "paw") UiMotifs.Paw(e.Graphics, new RectangleF(Width - 76, Height / 2f - 22, 48, 48), Color.FromArgb(237, 200, 216));
        else if (Motif == "bone") UiMotifs.Bone(e.Graphics, new RectangleF(Width - 92, Height / 2f - 12, 64, 32), Color.White, Color.FromArgb(201, 218, 233));
        if (LaceEdge)
        {
            using var lace = new SolidBrush(Color.FromArgb(255, 247, 251));
            using var seam = new Pen(Color.FromArgb(237, 199, 216));
            for (var x = 24f; x < Width - 42; x += 26)
            {
                using var pleat = new GraphicsPath();
                pleat.AddLines([new PointF(x + 3, 3), new PointF(x + 20, 3), new PointF(x + 23, 14)]);
                pleat.AddBezier(x + 23, 14, x + 16, 18, x + 7, 18, x, 14);
                pleat.CloseFigure();
                e.Graphics.FillPath(lace, pleat); e.Graphics.DrawPath(seam, pleat);
            }
        }
        base.OnPaint(e);
    }
}
