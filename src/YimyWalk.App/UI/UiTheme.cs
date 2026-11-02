using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI;

internal static class UiTheme
{
    public const int Space8 = 8;
    public const int Space12 = 12;
    public const int Space16 = 16;
    public const int Space24 = 24;
    public const int SidebarWidth = 216;

    public static readonly Color Ink = Color.FromArgb(91, 65, 81);
    public static readonly Color Cream = Color.FromArgb(255, 249, 251);
    public static readonly Color Sidebar = Color.FromArgb(255, 237, 244);
    public static readonly Color Yellow = Sidebar;
    public static readonly Color Coral = Color.FromArgb(249, 212, 227);
    public static readonly Color Teal = Color.FromArgb(190, 88, 129);
    public static readonly Color Muted = Color.FromArgb(139, 113, 129);
    public static readonly Color Panel = Color.White;
    public static readonly Color Border = Color.FromArgb(239, 218, 229);
    public static readonly Color NavInk = Color.FromArgb(148, 59, 97);
    public static readonly Color Blue = Color.FromArgb(115, 153, 186);
    public static readonly Color BlueWash = Color.FromArgb(238, 246, 253);
    public static readonly Color Gold = Color.FromArgb(196, 159, 97);
    public static readonly Color Preview = Color.FromArgb(255, 245, 249);

    private static readonly string UiFont = FontFamily.Families.Any(f => f.Name.Equals("Microsoft YaHei UI", StringComparison.OrdinalIgnoreCase))
        ? "Microsoft YaHei UI" : "Segoe UI";
    public static Font TitleFont { get; } = new(UiFont, 23f, FontStyle.Bold);
    public static Font BrandFont { get; } = new("Segoe UI", 21f, FontStyle.Bold);
    public static Font SectionFont { get; } = new(UiFont, 12f, FontStyle.Bold);
    public static Font BodyFont { get; } = new(UiFont, 9.5f, FontStyle.Regular);
    public static Font SmallFont { get; } = new(UiFont, 8.5f, FontStyle.Regular);
    public static Font NavFont { get; } = new(UiFont, 10f, FontStyle.Bold);
    public static Font NumberFont { get; } = new("Segoe UI", 25f, FontStyle.Bold);

    public static void ConfigureAntd()
    {
        AntdUI.Config.Mode = AntdUI.TMode.Light;
        AntdUI.Style.SetPrimary(Teal);
        AntdUI.Config.Animation = true;
    }

    public static AntdUI.Button CreateButton(string text, bool primary = false)
        => new()
        {
            Text = text,
            AutoSize = false,
            Width = Math.Max(primary ? 172 : 112, TextRenderer.MeasureText(text, BodyFont).Width + 54),
            Height = 48,
            Type = primary ? AntdUI.TTypeMini.Primary : AntdUI.TTypeMini.Default,
            DefaultBack = primary ? Teal : Color.White,
            DefaultBorderColor = primary ? Teal : Border,
            BorderWidth = primary ? 0 : 1,
            MinimumSize = new Size(primary ? 152 : 112, 44),
            WaveSize = 0,
            ForeColor = primary ? Color.White : Ink,
            Font = BodyFont,
            Padding = Padding.Empty,
            Radius = 14,
            Margin = new Padding(0, 0, 10, 10),
            TabStop = true
        };

    public static AntdUI.Button CreateNavButton(string text)
        => new()
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Height = 48,
            WaveSize = 0,
            Dock = DockStyle.Top,
            Type = AntdUI.TTypeMini.Default,
            DefaultBack = Sidebar,
            DefaultBorderColor = Color.Transparent,
            ForeColor = Ink,
            Font = NavFont,
            Padding = Padding.Empty,
            Radius = 16,
            Margin = Padding.Empty,
            TabStop = true
        };

    public static Label CreateBodyLabel(string text = "")
        => new()
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = BodyFont,
            ForeColor = Ink,
            Margin = new Padding(0, 3, 8, 3)
        };

    public static Label CreateMutedLabel(string text = "")
        => new()
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = SmallFont,
            ForeColor = Muted,
            Margin = new Padding(0, 3, Space8, 3)
        };

    public static UiCard CreateCard()
        => new()
        {
            SurfaceColor = Panel,
            Padding = new Padding(22),
            Margin = new Padding(0, 0, 0, 16)
        };

    public static AntdUI.Switch CreateSwitch(bool isChecked = false)
        => new()
        {
            Checked = isChecked,
            AutoSize = false,
            Size = new Size(50, 28),
            Fill = Teal,
            CheckedText = "",
            UnCheckedText = "",
            ForeColor = Color.White,
            Font = SmallFont,
            TabStop = true,
            Margin = new Padding(4, 4, 0, 4)
        };

    public static AntdUI.Select CreateSelect(int width = 220)
        => new()
        {
            Width = width,
            Height = 40,
            WaveSize = 0,
            Font = BodyFont,
            ForeColor = Ink,
            DropDownArrow = true,
            Radius = 12,
            TabStop = true,
            Margin = new Padding(4, 2, 0, 2)
        };

    public static AntdUI.InputNumber CreateInputNumber(decimal minimum, decimal maximum, decimal increment, int decimals, int width = 120)
        => new()
        {
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            DecimalPlaces = decimals,
            Width = width,
            Height = 40,
            WaveSize = 0,
            Font = BodyFont,
            ForeColor = Ink,
            AlwaysShowControl = true,
            Radius = 12,
            TabStop = true,
            Margin = new Padding(4, 2, 0, 2)
        };

    public static Control WrapSwitch(Control editor)
    {
        if (editor is not AntdUI.Switch toggle) return editor;
        var state = CreateMutedLabel(toggle.Checked ? "开启" : "关闭");
        state.Margin = new Padding(4, 7, 0, 3);
        toggle.CheckedChanged += (_, e) => state.Text = e.Value ? "开启" : "关闭";
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.Controls.Add(toggle);
        row.Controls.Add(state);
        return row;
    }

    public static void StylePage(Control page)
    {
        page.BackColor = Cream;
        page.ForeColor = Ink;
        page.Font = BodyFont;
    }

    public static Label SectionTitle(string text) => new()
    {
        Text = text, AutoSize = true, Font = SectionFont, ForeColor = Ink,
        BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 14)
    };
}
