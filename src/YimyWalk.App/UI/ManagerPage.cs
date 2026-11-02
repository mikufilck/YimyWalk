using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI;

internal enum ManagerPageId
{
    Overview,
    Pets,
    Characters,
    Interactions,
    Settings
}

internal abstract class ManagerPage : UserControl
{
    protected ManagerPage(AppHost host)
    {
        Host = host;
        UiTheme.StylePage(this);
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Padding = new Padding(28, 24, 28, 24);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        TabStop = true;
    }

    protected AppHost Host { get; }

    public abstract string PageTitle { get; }

    /// <summary>Refreshes values owned by AppHost without rebuilding the page.</summary>
    public virtual void RefreshFromHost() { }

    protected TableLayoutPanel CreateRootLayout()
    {
        var layout = UiLayout.Stack();
        Controls.Add(layout);
        return layout;
    }

    protected Panel AddHeading(TableLayoutPanel layout, string title, string subtitle = "")
    {
        var heading = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 20),
            MinimumSize = Size.Empty
        };
        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = UiTheme.TitleFont,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.Ink,
            Location = new Point(0, 0)
        };
        var subtitleLabel = new Label
        {
            Text = subtitle,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Font = UiTheme.BodyFont,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.Muted,
            Location = new Point(2, 0)
        };
        var titleHeight = titleLabel.GetPreferredSize(Size.Empty).Height;
        subtitleLabel.Location = new Point(2, titleHeight + UiTheme.Space8);
        var subtitleHeight = subtitleLabel.GetPreferredSize(new Size(760, 0)).Height;
        heading.MinimumSize = new Size(0, string.IsNullOrWhiteSpace(subtitle) ? titleHeight + UiTheme.Space8 : subtitleLabel.Top + subtitleHeight + UiTheme.Space8);
        if (!string.IsNullOrWhiteSpace(subtitle)) heading.Controls.Add(subtitleLabel);
        else subtitleLabel.Dispose();
        heading.Controls.Add(titleLabel);
        heading.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var size = 20 * heading.DeviceDpi / 96f;
            UiMotifs.Paw(e.Graphics, new RectangleF(titleLabel.Right + size / 2, titleLabel.Top + (titleLabel.Height - size) / 2, size, size), UiTheme.Coral);
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(heading, 0, layout.RowCount++);
        return heading;
    }

    protected void AddRow(TableLayoutPanel layout, Control control)
    {
        UiLayout.AddRow(layout, control);
    }
}
