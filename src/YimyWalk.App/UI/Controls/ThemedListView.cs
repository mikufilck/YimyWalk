namespace YimyWalk.App.UI.Controls;

internal sealed class ThemedListView : ListView
{
    private readonly ImageList _rowHeight = new() { ImageSize = new Size(1, 40) };
    private float[] _columnWeights = Array.Empty<float>();
    public ThemedListView()
    {
        DoubleBuffered = true; OwnerDraw = true; BorderStyle = BorderStyle.None;
        View = View.Details; FullRowSelect = true; HideSelection = false;
        MultiSelect = false; HeaderStyle = ColumnHeaderStyle.Nonclickable;
        Dock = DockStyle.Fill;
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
    internal void SetColumns(params (string Text, float Weight)[] columns)
    {
        Columns.Clear();
        foreach (var column in columns) Columns.Add(column.Text);
        _columnWeights = columns.Select(column => column.Weight).ToArray();
        ResizeColumns();
    }
    protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); ResizeColumns(); }
    private void ResizeColumns()
    {
        if (_columnWeights is not { Length: > 0 }) return;
        var total = _columnWeights.Sum();
        if (total <= 0) return;
        for (var i = 0; i < _columnWeights.Length; i++)
            Columns[i].Width = Math.Max(48, (int)((ClientSize.Width - 6) * _columnWeights[i] / total));
    }
    protected override void Dispose(bool disposing) { if (disposing) _rowHeight.Dispose(); base.Dispose(disposing); }
}
