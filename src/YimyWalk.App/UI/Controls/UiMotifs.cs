using System.Drawing.Drawing2D;

namespace YimyWalk.App.UI.Controls;

/// <summary>Code-native UI ornaments; character artwork is never modified.</summary>
internal static class UiMotifs
{
    internal static void Paw(Graphics graphics, RectangleF bounds, Color color)
    {
        using var brush = new SolidBrush(color);
        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(bounds.X, bounds.Y);
            graphics.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
            graphics.FillEllipse(brush, 2, 6, 4, 6);
            graphics.FillEllipse(brush, 7, 2, 4, 6);
            graphics.FillEllipse(brush, 13, 2, 4, 6);
            graphics.FillEllipse(brush, 18, 6, 4, 6);
            graphics.FillEllipse(brush, 6, 11, 12, 10);
        }
        finally { graphics.Restore(state); }
    }

    internal static GraphicsPath Round(RectangleF r, float radius)
    {
        var p = new GraphicsPath(); var d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure(); return p;
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
