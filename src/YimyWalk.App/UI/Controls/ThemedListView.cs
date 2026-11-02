namespace YimyWalk.App.UI.Controls;

internal sealed class ThemedListView : ListView
{
    private readonly ImageList _rowHeight = new() { ImageSize = new Size(1, 40) };
    public ThemedListView()
    {
        DoubleBuffered = true; OwnerDraw = true; BorderStyle = BorderStyle.None;
        BackColor = Color.White; ForeColor = UiTheme.Ink; Font = UiTheme.BodyFont; SmallImageList = _rowHeight;
        DrawColumnHeader += (_, e) =>
        {
            using var brush = new SolidBrush(UiTheme.Preview); e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", UiTheme.SmallFont, Rectangle.Inflate(e.Bounds, -10, 0), UiTheme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        DrawItem += (_, e) => { if (View != View.Details) e.DrawDefault = true; };
        DrawSubItem += (_, e) =>
        {
            if (e.Item is null || e.SubItem is null) return;
            using var brush = new SolidBrush(e.Item.Selected ? UiTheme.Sidebar : Color.White); e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Font, Rectangle.Inflate(e.Bounds, -10, 0), e.Item.Selected ? UiTheme.NavInk : ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };
    }
    protected override void Dispose(bool disposing) { if (disposing) _rowHeight.Dispose(); base.Dispose(disposing); }
}
