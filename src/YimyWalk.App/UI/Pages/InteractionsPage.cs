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
    private bool _syncing;

    public InteractionsPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "互动", "设置桌宠的默认行为，单只桌宠可在「桌宠」页调整。");
        var pair = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1 };
        pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var toggles = new SettingRows();
        toggles.Margin = new Padding(0, 0, 28, 0);
        toggles.Add("主动走动", "", _walk);
        toggles.Add("自动气泡", "", _defaultDialogue);
        toggles.Add("饥饿增长", "", _hunger);
        toggles.Add("应用切换反馈", "切换前台应用时触发台词，默认关闭。", _external);
        // Keep the total gate after the four defaults in the keyboard order.
        toggles.Add("自动气泡总开关", "关闭后，所有桌宠都不显示自动气泡。", _dialogueGate);
        pair.Controls.Add(toggles, 0, 0);
        var behavior = new SettingRows();
        behavior.Add("活跃度倍率", "影响自主活动频率，不改变移动速度。", _activity);
        behavior.Add("跳跃倾向 (%)", "0 表示不主动跳跃。", _jumpDesire);
        behavior.Add("攀爬倾向 (%)", "0 表示不主动攀爬。", _climbDesire);
        behavior.Add("窗口跳跃倾向 (%)", "跳到可见窗口顶部的意愿。", _windowJumpDesire);
        pair.Controls.Add(behavior, 1, 0); AddRow(layout, pair);
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
        }
        finally { _syncing = false; }
    }
}
