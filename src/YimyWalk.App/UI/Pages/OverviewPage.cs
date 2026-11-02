using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI.Pages;

internal sealed class OverviewPage : ManagerPage
{
    private readonly Label _active = UiTheme.CreateBodyLabel();
    private readonly Label _pending = UiTheme.CreateBodyLabel();
    private readonly Label _settings = UiTheme.CreateBodyLabel();
    private readonly Label _operation = UiTheme.CreateMutedLabel();
    private readonly Label _defaultRole = UiTheme.CreateMutedLabel();
    private readonly AntdUI.Select _createCount = UiTheme.CreateSelect(88);
    private readonly AntdUI.Button _create = UiTheme.CreateButton("创建桌宠", primary: true);
    private readonly AntdUI.Button _pause = UiTheme.CreateButton("暂停桌宠");

    public OverviewPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "欢迎回到小窝", "今天的桌面，也要有一点软乎乎的陪伴。");
        var hero = UiTheme.CreateCard();
        hero.Name = "CompanionHero";
        hero.SurfaceColor = UiTheme.Sidebar;
        hero.LaceEdge = true;
        hero.Height = LogicalToDeviceUnits(230);
        hero.Padding = new Padding(26, 22, 20, 18);
        var composition = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = Color.Transparent };
        composition.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        composition.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        var words = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Margin = Padding.Empty };
        words.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++) words.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        words.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var eyebrow = UiTheme.CreateMutedLabel("YIMY'S LITTLE WORLD"); eyebrow.ForeColor = UiTheme.NavInk;
        var title = UiTheme.CreateBodyLabel("今天，也要被可爱包围。");
        title.Font = UiTheme.TitleFont; title.Margin = new Padding(0, 8, 0, 10);
        title.MaximumSize = new Size(650, 0);
        var description = UiTheme.CreateMutedLabel("咿米已经准备好，陪你慢慢度过今天。");
        description.Margin = new Padding(0, 0, 0, 12);
        _defaultRole.Margin = new Padding(0, 0, 0, 8);
        var createRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = Padding.Empty };
        _createCount.Items.AddRange(new object[] { "1", "10", "50", "100" });
        _createCount.SelectedIndex = 0;
        _createCount.AccessibleName = "创建数量";
        _createCount.Margin = new Padding(0, 0, 10, 0);
        _create.AccessibleName = "创建桌宠"; _create.IconSvg = UiMotifs.Icon("paw");
        _create.Click += (_, _) =>
        {
            var count = int.TryParse(_createCount.SelectedValue?.ToString(), out var selected) ? selected : 1;
            Host.QueueCreate(Host.SelectedRoleId, count); RefreshFromHost();
        };
        createRow.Controls.Add(_createCount); createRow.Controls.Add(_create);
        words.Controls.Add(eyebrow, 0, 0); words.Controls.Add(title, 0, 1);
        words.Controls.Add(description, 0, 2); words.Controls.Add(_defaultRole, 0, 3); words.Controls.Add(createRow, 0, 4);
        composition.Controls.Add(words, 0, 0);
        composition.Controls.Add(new CompanionArt { Dock = DockStyle.Fill, Name = "HeroPortrait" }, 1, 0);
        hero.Controls.Add(composition); AddRow(layout, hero);

        var metrics = new TableLayoutPanel { Name = "DesktopMetrics", Dock = DockStyle.Top, Height = LogicalToDeviceUnits(100), ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 0, 0, 16) };
        for (var i = 0; i < 3; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        metrics.Controls.Add(Metric("正在陪伴", _active, UiTheme.Preview, "只桌宠，让桌面热闹一点"), 0, 0);
        metrics.Controls.Add(Metric("等待出发", _pending, UiTheme.BlueWash, "正在排队来到你的桌面"), 1, 0);
        metrics.Controls.Add(Metric("安心记住", _settings, Color.White, "偏好会自动保存"), 2, 0);
        AddRow(layout, metrics);

        var actions = UiTheme.CreateCard(); actions.Height = Math.Max(220, LogicalToDeviceUnits(160));
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 3; i++) content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(UiTheme.SectionTitle("陪伴小日常"), 0, 0);
        var daily = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = Padding.Empty };
        void AddAction(FlowLayoutPanel row, string text, string icon, Action action)
        {
            var button = UiTheme.CreateButton(text); button.IconSvg = UiMotifs.Icon(icon);
            button.Click += (_, _) => action(); row.Controls.Add(button);
        }
        AddAction(daily, "喂食全部", "bone", () => Host.FeedAll());
        AddAction(daily, "投放食物", "bone", () => Host.SpawnVirtualFood());
        _pause.Click += (_, _) => { if (Host.IsPaused) Host.ResumePets(); else Host.PausePets(); RefreshFromHost(); };
        daily.Controls.Add(_pause);
        AddAction(daily, "隐藏桌宠", "paw", () => Host.SetPetsVisible(false));
        AddAction(daily, "显示桌宠", "paw", () => Host.SetPetsVisible(true));
        content.Controls.Add(daily, 0, 1);
        var batch = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = Padding.Empty };
        foreach (var count in new[] { 1, 10, 50, 100 })
        {
            var button = UiTheme.CreateButton($"创建 {count}");
            button.AccessibleName = $"创建 {count} 只桌宠";
            button.Click += (_, _) => Host.QueueCreate(Host.SelectedRoleId, count); batch.Controls.Add(button);
        }
        var clear = UiTheme.CreateButton("一键清空");
        clear.ForeColor = UiTheme.NavInk; clear.Click += (_, _) => Host.ClearPets(); batch.Controls.Add(clear);
        content.Controls.Add(batch, 0, 2); content.Controls.Add(_operation, 0, 3);
        actions.Controls.Add(content); AddRow(layout, actions);
    }

    private static UiCard Metric(string title, Label value, Color color, string note)
    {
        var card = UiTheme.CreateCard(); card.Dock = DockStyle.Fill;
        card.SurfaceColor = color; card.Padding = new Padding(18, 12, 12, 12); card.Margin = new Padding(0, 0, 12, 0);
        card.Motif = title == "正在陪伴" ? "paw" : title == "等待出发" ? "bone" : null;
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        rows.Controls.Add(UiTheme.CreateMutedLabel(title), 0, 0);
        value.Font = UiTheme.NumberFont;
        if (title == "安心记住") value.Font = UiTheme.SectionFont;
        value.Margin = Padding.Empty; rows.Controls.Add(value, 0, 1);
        rows.Controls.Add(UiTheme.CreateMutedLabel(note), 0, 2); card.Controls.Add(rows); return card;
    }

    public override string PageTitle => "总览";
    public override void RefreshFromHost()
    {
        _active.Text = Host.ActivePetCount.ToString();
        _pending.Text = Host.PendingCreationCount.ToString();
        _settings.Text = UiText.SaveStateLabel(Host.SettingsSaveState);
        _defaultRole.Text = $"这次邀请：{(Host.Catalog.TryGet(Host.SelectedRoleId, out var role) ? UiText.CharacterName(role.DisplayName) : Host.SelectedRoleId)}";
        _pause.Text = Host.IsPaused ? "继续桌宠" : "暂停桌宠";
        _operation.Text = Host.RuntimeDiagnostic ?? (Host.PendingCreationCount > 0 ? "正在创建桌宠…" : string.Empty);
    }
}
