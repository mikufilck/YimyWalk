namespace YimyWalk.App.UI.Controls;

/// <summary>Content-sized settings rows, keeping prose and editors in separate columns.</summary>
internal sealed class SettingRows : TableLayoutPanel
{
    internal SettingRows(int editorWidth = 130)
    {
        Dock = DockStyle.Top; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; ColumnCount = 2; Margin = Padding.Empty;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, editorWidth));
    }
    internal void Add(string title, string note, Control editor)
    {
        var row = RowCount++; RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var prose = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = new Padding(0, 12, 20, 14) };
        prose.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var heading = UiTheme.CreateBodyLabel(title); heading.Font = UiTheme.SectionFont;
        var detail = UiTheme.CreateMutedLabel(note);
        prose.Controls.Add(heading, 0, 0); prose.Controls.Add(detail, 0, 1);
        prose.SizeChanged += (_, _) =>
        {
            var width = Math.Max(1, prose.ClientSize.Width - 8);
            heading.MaximumSize = new Size(width, 0); detail.MaximumSize = new Size(width, 0);
        };
        Controls.Add(prose, 0, row);
        var wrapped = UiTheme.WrapSwitch(editor);
        wrapped.Anchor = AnchorStyles.Top | AnchorStyles.Right; wrapped.Margin = new Padding(0, 16, 0, 12);
        Controls.Add(wrapped, 1, row);
    }
    internal UiCard InCard(string title)
    {
        var card = UiTheme.CreateCard(); card.AutoSize = true; card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var stack = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, ColumnCount = 1, Margin = Padding.Empty };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.Controls.Add(UiTheme.SectionTitle(title), 0, 0); stack.Controls.Add(this, 0, 1);
        card.Controls.Add(stack); return card;
    }
}
