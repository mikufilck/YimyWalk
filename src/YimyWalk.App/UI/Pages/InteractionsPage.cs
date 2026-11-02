using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI.Pages;

internal sealed class InteractionsPage : ManagerPage
{
    private readonly AntdUI.Switch _walk = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _defaultDialogue = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _dialogueGate = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _hunger = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _external = UiTheme.CreateSwitch();
    private readonly AntdUI.InputNumber _activity = UiTheme.CreateInputNumber(0.1m, 4m, 0.1m, 1, 120);
    private readonly AntdUI.InputNumber _jumpDesire = UiTheme.CreateInputNumber(0m, 100m, 1m, 0, 120);
    private readonly AntdUI.InputNumber _climbDesire = UiTheme.CreateInputNumber(0m, 100m, 1m, 0, 120);
    private readonly AntdUI.InputNumber _windowJumpDesire = UiTheme.CreateInputNumber(0m, 100m, 1m, 0, 120);
    private readonly Label _status = UiTheme.CreateMutedLabel();
    private bool _syncing;

    public InteractionsPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "互动", "把她的小日常，调成你喜欢的节奏。");
        var pair = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 16) };
        pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var company = new SettingRows();
        company.Add("默认主动走动", "让她自己走走，探索你的桌面。", _walk);
        company.Add("默认自动气泡", "不时冒出一句话，分享一点心情。", _defaultDialogue);
        company.Add("饥饿增长", "时间久了会饿，记得给她一点小零食。", _hunger);
        company.Add("应用切换反馈", "切换应用时，也听听她想说什么。", _external);
        var companyCard = company.InCard("陪伴的方式"); companyCard.Margin = new Padding(0, 0, 12, 0);
        pair.Controls.Add(companyCard, 0, 0); companyCard.Dock = DockStyle.Fill;

        var personality = new SettingRows();
        personality.Add("默认活跃度倍率", "越高越爱活动，不改变走路速度。", _activity);
        personality.Add("跳跃欲望 (%)", "想跳就跳，设为 0 时安静走走。", _jumpDesire);
        personality.Add("攀爬欲望 (%)", "沿着屏幕边缘，一点一点往上爬。", _climbDesire);
        personality.Add("窗口跳跃欲望 (%)", "试着跳到窗口上方，换个地方看世界。", _windowJumpDesire);
        var personalityCard = personality.InCard("一点小脾气"); personalityCard.Margin = new Padding(0, 0, 0, 0);
        pair.Controls.Add(personalityCard, 1, 0); personalityCard.Dock = DockStyle.Fill;
        AddRow(layout, pair);

        var quiet = new SettingRows();
        quiet.Add("允许自动气泡", "想安静一会儿？关闭后，所有桌宠都不再自动说话。", _dialogueGate);
        AddRow(layout, quiet.InCard("留一点安静"));
        AddRow(layout, _status);
        _walk.CheckedChanged += (_, _) => { if (!_syncing) Host.DefaultWalkEnabled = _walk.Checked; };
        _defaultDialogue.CheckedChanged += (_, _) => { if (!_syncing) Host.DefaultDialogueEnabled = _defaultDialogue.Checked; };
        _dialogueGate.CheckedChanged += (_, _) => { if (!_syncing) Host.DialogueEnabled = _dialogueGate.Checked; };
        _hunger.CheckedChanged += (_, _) => { if (!_syncing) Host.HungerEnabled = _hunger.Checked; };
        _external.CheckedChanged += (_, _) => { if (!_syncing) Host.ExternalEventsEnabled = _external.Checked; };
        _activity.ValueChanged += (_, _) => { if (!_syncing) Host.ActivityRate = (float)_activity.Value; };
        _jumpDesire.ValueChanged += (_, _) => { if (!_syncing) Host.JumpDesire = (float)_jumpDesire.Value / 100f; };
        _climbDesire.ValueChanged += (_, _) => { if (!_syncing) Host.ClimbDesire = (float)_climbDesire.Value / 100f; };
        _windowJumpDesire.ValueChanged += (_, _) => { if (!_syncing) Host.WindowJumpDesire = (float)_windowJumpDesire.Value / 100f; };
    }
    public override string PageTitle => "互动";
    public override void RefreshFromHost()
    {
        _syncing = true;
        try
        {
            _walk.Checked = Host.DefaultWalkEnabled; _defaultDialogue.Checked = Host.DefaultDialogueEnabled;
            _dialogueGate.Checked = Host.DialogueEnabled; _hunger.Checked = Host.HungerEnabled; _external.Checked = Host.ExternalEventsEnabled;
            _activity.Value = (decimal)Host.ActivityRate; _jumpDesire.Value = (decimal)(Host.JumpDesire * 100f);
            _climbDesire.Value = (decimal)(Host.ClimbDesire * 100f); _windowJumpDesire.Value = (decimal)(Host.WindowJumpDesire * 100f);
            _status.Text = $"设置：{UiText.SaveStateLabel(Host.SettingsSaveState)}";
        }
        finally { _syncing = false; }
    }
}
