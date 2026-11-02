using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI.Pages;

internal sealed class OverviewPage : ManagerPage
{
    private readonly Label _active = UiTheme.CreateBodyLabel();
    private readonly Label _pending = UiTheme.CreateMutedLabel();
    private readonly Label _operation = UiTheme.CreateMutedLabel();
    private readonly Label _defaultRole = UiTheme.CreateBodyLabel();
    private readonly AntdUI.Select _createCount = UiTheme.CreateSelect(88);
    private readonly AntdUI.Button _create = UiTheme.CreateButton("创建桌宠", primary: true);
    private readonly AntdUI.Button _pause = UiTheme.CreateButton("暂停桌宠");
    private readonly CharacterFramePreview _preview;

    public OverviewPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "总览");
        var status = Flow();
        status.Controls.Add(_active); status.Controls.Add(_pending);
        AddRow(layout, status);

        var creation = new TableLayoutPanel
        {
            Name = "CreatePet", Dock = DockStyle.Top, Height = LogicalToDeviceUnits(190),
            ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 16, 0, 20)
        };
        creation.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(200)));
        creation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Margin = new Padding(24, 12, 0, 0) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _defaultRole.Font = UiTheme.SectionFont;
        _defaultRole.Margin = new Padding(0, 12, 0, 18);
        form.Controls.Add(_defaultRole, 0, 0);
        var createRow = Flow();
        var quantity = UiTheme.CreateBodyLabel("数量");
        quantity.Margin = new Padding(0, 12, 12, 0);
        _createCount.Items.AddRange(new object[] { "1", "10", "50", "100" });
        _createCount.SelectedIndex = 0; _createCount.AccessibleName = "创建数量";
        _createCount.Margin = new Padding(0, 4, 12, 0);
        _create.AccessibleName = "创建桌宠"; _create.IconSvg = UiMotifs.Icon("paw");
        _create.Click += (_, _) =>
        {
            var count = int.TryParse(_createCount.SelectedValue?.ToString(), out var selected) ? selected : 1;
            Host.QueueCreate(Host.SelectedRoleId, count); RefreshFromHost();
        };
        createRow.Controls.Add(quantity); createRow.Controls.Add(_createCount); createRow.Controls.Add(_create);
        form.Controls.Add(createRow, 0, 1); creation.Controls.Add(form, 1, 0);
        _preview = new CharacterFramePreview(host.SurfaceCache) { Dock = DockStyle.Fill, Name = "DefaultRolePreview" };
        creation.Controls.Add(_preview, 0, 0);
        AddRow(layout, creation);

        var actions = Flow();
        void AddAction(string text, Action action)
        {
            var button = UiTheme.CreateButton(text);
            button.Click += (_, _) => action(); actions.Controls.Add(button);
        }
        AddAction("喂食全部", () => Host.FeedAll());
        AddAction("投放食物", () => Host.SpawnVirtualFood());
        _pause.Click += (_, _) => { if (Host.IsPaused) Host.ResumePets(); else Host.PausePets(); RefreshFromHost(); };
        actions.Controls.Add(_pause);
        AddAction("隐藏桌宠", () => Host.SetPetsVisible(false));
        AddAction("显示桌宠", () => Host.SetPetsVisible(true));
        AddAction("一键清空", () => Host.ClearPets());
        AddRow(layout, actions); AddRow(layout, _operation);
    }

    private static FlowLayoutPanel Flow() => new()
    {
        Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true, Margin = Padding.Empty
    };
    public override string PageTitle => "总览";
    public override void RefreshFromHost()
    {
        _active.Text = $"桌宠：{Host.ActivePetCount} 只{(Host.IsPaused ? " · 已暂停" : "")}";
        _pending.Text = $"待创建：{Host.PendingCreationCount} 只";
        _pending.Visible = Host.PendingCreationCount > 0;
        if (Host.Catalog.TryGet(Host.SelectedRoleId, out var role))
        {
            _defaultRole.Text = $"默认角色：{UiText.CharacterName(role.DisplayName)}";
            _preview.ShowIdleFrame(role);
        }
        else { _defaultRole.Text = "没有可用角色"; _preview.ShowIdleFrame(null); }
        _pause.Text = Host.IsPaused ? "继续桌宠" : "暂停桌宠";
        _operation.Text = Host.RuntimeDiagnostic ?? string.Empty;
        _operation.Visible = _operation.Text.Length > 0;
    }
}
