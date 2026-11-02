namespace YimyWalk.App.UI.Controls;

internal static class UiLayout
{
    internal static FlowLayoutPanel Flow() => new()
    {
        Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true, Margin = Padding.Empty
    };

    internal static TableLayoutPanel Stack()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, RowCount = 0, Margin = Padding.Empty, Padding = Padding.Empty
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return stack;
    }

    internal static void AddRow(TableLayoutPanel stack, Control content)
    {
        content.Dock = DockStyle.Top;
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.Controls.Add(content, 0, stack.RowCount++);
    }

    internal static void ListDetail(TableLayoutPanel root, Control list, Control detail)
    {
        root.Dock = DockStyle.Fill; root.AutoSize = false;
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            MinimumSize = new Size(0, 430), Margin = Padding.Empty
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        list.Dock = detail.Dock = DockStyle.Fill;
        list.Margin = new Padding(0, 0, 14, 0); detail.Margin = Padding.Empty;
        split.Controls.Add(list, 0, 0); split.Controls.Add(detail, 1, 0);
        AddRow(root, split); split.Dock = DockStyle.Fill;
        root.RowStyles[^1] = new RowStyle(SizeType.Percent, 100);
    }
}
