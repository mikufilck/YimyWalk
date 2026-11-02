using System.Drawing.Drawing2D;

namespace YimyWalk.App.UI.Controls;

/// <summary>Code-native UI ornaments; character artwork is never modified.</summary>
internal static class UiMotifs
{
    internal static GraphicsPath Round(RectangleF r, float radius)
    {
        var p = new GraphicsPath(); var d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }
    internal static void Paw(Graphics g, RectangleF r, Color color)
    {
        var saved = g.Save(); g.TranslateTransform(r.X, r.Y); g.ScaleTransform(r.Width / 32, r.Height / 32);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 9, 7, 10); g.FillEllipse(brush, 9, 2, 7, 10);
        g.FillEllipse(brush, 18, 2, 7, 10); g.FillEllipse(brush, 25, 9, 7, 10);
        using var pad = new GraphicsPath();
        pad.AddBezier(7, 26, 7, 20, 12, 14, 17, 14); pad.AddBezier(17, 14, 22, 14, 27, 21, 27, 26);
        pad.AddBezier(27, 26, 27, 32, 20, 28, 17, 28); pad.AddBezier(17, 28, 12, 28, 7, 32, 7, 26);
        g.FillPath(brush, pad); g.Restore(saved);
    }
    internal static void Bone(Graphics g, RectangleF r, Color fill, Color line)
    {
        var saved = g.Save(); g.TranslateTransform(r.X, r.Y); g.ScaleTransform(r.Width / 48, r.Height / 24);
        using var p = new GraphicsPath();
        p.AddBezier(12, 7, 7, -4, -3, 3, 3, 12); p.AddBezier(3, 12, -3, 22, 8, 29, 12, 17);
        p.AddLine(12, 17, 36, 17); p.AddBezier(36, 17, 41, 29, 52, 22, 45, 12);
        p.AddBezier(45, 12, 52, 3, 41, -4, 36, 7); p.CloseFigure();
        using var b = new SolidBrush(fill); using var pen = new Pen(line, 1.3f);
        g.FillPath(b, p); g.DrawPath(pen, p); g.Restore(saved);
    }
    internal static string Icon(string kind)
    {
        var shape = kind switch
        {
            "home" => "<path d='M3 11 12 3l9 8v9a1 1 0 0 1-1 1h-6v-7h-4v7H4a1 1 0 0 1-1-1z'/>",
            "paw" => "<ellipse cx='5' cy='9' rx='2' ry='3'/><ellipse cx='10' cy='5' rx='2' ry='3'/><ellipse cx='16' cy='5' rx='2' ry='3'/><ellipse cx='21' cy='9' rx='2' ry='3'/><path d='M7 19c-4-5 3-9 6-9s10 5 6 9c-2 2-4 0-6 0s-4 2-6 0z'/>",
            "heart" => "<path d='M12 21C-5 10 6-1 12 6 18-1 29 10 12 21z'/>",
            "bone" => "<path d='M7 8C3 0-2 8 3 12c-5 5 1 12 4 4h10c3 8 9 1 4-4 5-4 0-12-4-4z'/>",
            "settings" => "<path d='M4 6h16M4 12h16M4 18h16'/><circle cx='9' cy='6' r='2.5' fill='white'/><circle cx='16' cy='12' r='2.5' fill='white'/><circle cx='8' cy='18' r='2.5' fill='white'/>",
            _ => "<path d='M4 6 8 9h8l4-3v9c0 8-16 8-16 0z'/><path d='M9 14h.01M15 14h.01M11 18h2'/>"
        };
        return $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.7' stroke-linecap='round' stroke-linejoin='round'>{shape}</svg>";
    }
}
