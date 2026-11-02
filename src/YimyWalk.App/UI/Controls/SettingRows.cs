namespace YimyWalk.App.UI.Controls;

/// <summary>Content-sized settings rows, keeping prose and editors in separate columns.</summary>
internal sealed class SettingRows : TableLayoutPanel
{
    internal SettingRows(int editorWidth = 130)
    {
        Dock = DockStyle.Top; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; ColumnCount = 2; Margin = Padding.Empty;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, editorWidth));
    }
    internal void Add(string title, string note, Control editor, Label? status = null)
    {
        var row = RowCount++; RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var prose = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = new Padding(0, 12, 20, 14) };
        prose.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var heading = UiTheme.CreateBodyLabel(title);
        var detail = UiTheme.CreateMutedLabel(note);
        prose.Controls.Add(heading, 0, 0);
        if (!string.IsNullOrWhiteSpace(note)) prose.Controls.Add(detail, 0, 1);
        else detail.Dispose();
        if (status is not null) prose.Controls.Add(status, 0, string.IsNullOrWhiteSpace(note) ? 1 : 2);
        editor.AccessibleName ??= title;
        if (editor is NumberOverrideEditor number) number.SetAccessibleLabel(title);
        prose.SizeChanged += (_, _) =>
        {
            var width = Math.Max(1, prose.ClientSize.Width - 8);
            heading.MaximumSize = new Size(width, 0);
            if (!detail.IsDisposed) detail.MaximumSize = new Size(width, 0);
            if (status is not null) status.MaximumSize = new Size(width, 0);
        };
        Controls.Add(prose, 0, row);
        var wrapped = UiTheme.WrapSwitch(editor);
        wrapped.Anchor = AnchorStyles.Top | AnchorStyles.Right; wrapped.Margin = new Padding(0, 16, 0, 12);
        Controls.Add(wrapped, 1, row);
    }
}
