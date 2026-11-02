using YimyWalk.App.Host;
using YimyWalk.App.Settings;
using YimyWalk.App.UI.Controls;
using YimyWalk.Core.Display;

namespace YimyWalk.App.UI.Pages;

internal sealed class SettingsPage : ManagerPage
{
    private readonly AntdUI.InputNumber _scale = UiTheme.CreateInputNumber(0.1m, 8m, 0.1m, 1, 120);
    private readonly AntdUI.Select _display = UiTheme.CreateSelect(300);
    private readonly AntdUI.Switch _landing = UiTheme.CreateSwitch();
    private readonly Label _status = UiTheme.CreateBodyLabel();
    private readonly Label _diagnostic = UiTheme.CreateMutedLabel();
    private readonly AntdUI.Button _retry = UiTheme.CreateButton("重试保存");
    private bool _syncing;

    public SettingsPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "设置", "把小窝布置成，你最舒服的样子。");
        var display = new SettingRows(320);
        display.Add("桌宠显示缩放", "大一点抱抱，小一点陪伴。0.1–8.0，修改后立即生效。", _scale);
        display.Add("新桌宠出现的位置", "挑一块屏幕，作为这次相遇的起点。", _display);
        display.Add("允许落到可见窗口顶部", "让她停在窗口边缘，陪你一起看世界。", _landing);
        AddRow(layout, display.InCard("她的桌面舞台"));

        var save = UiTheme.CreateCard(); save.SurfaceColor = UiTheme.BlueWash;
        save.AutoSize = true; save.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var rows = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, Margin = Padding.Empty };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rows.Controls.Add(UiTheme.SectionTitle("偏好会被好好记住"), 0, 0);
        _status.Font = UiTheme.SectionFont;
        rows.Controls.Add(_status, 0, 1); rows.Controls.Add(_diagnostic, 0, 2);
        rows.Controls.Add(_retry, 0, 3);
        rows.SizeChanged += (_, _) =>
        {
            var width = Math.Max(1, rows.ClientSize.Width - 8);
            _status.MaximumSize = new Size(width, 0); _diagnostic.MaximumSize = new Size(width, 0);
        };
        save.Controls.Add(rows); AddRow(layout, save);
        _retry.Click += (_, _) => Host.RetrySettingsSave();
        _scale.ValueChanged += (_, _) => { if (!_syncing) Host.GlobalScale = (float)_scale.Value; };
        _display.SelectedIndexChanged += (_, _) =>
        {
            if (!_syncing && _display.SelectedValue is DisplayChoice choice) Host.SetPreferredDisplay(choice.Id);
        };
        _landing.CheckedChanged += (_, _) => { if (!_syncing) Host.WindowLandingEnabled = _landing.Checked; };
    }
    public override string PageTitle => "设置";
    public override void RefreshFromHost()
    {
        _syncing = true;
        try
        {
            _scale.Value = (decimal)Host.GlobalScale; _landing.Checked = Host.WindowLandingEnabled;
            RefreshDisplaysIfNeeded();
            RefreshStatus($"设置：{UiText.SaveStateLabel(Host.SettingsSaveState)}");
            var load = Host.SettingsLoad.Diagnostic;
            var firstRun = load == "主设置文件：文件不存在；备份设置文件：文件不存在" || load == "设置文件不存在，已使用默认值";
            _diagnostic.Text = Host.SettingsSaveError is { Length: > 0 } error ? $"保存错误：{error}"
                : firstRun || string.IsNullOrWhiteSpace(load) ? "不用找保存按钮，修改后会自动记住。"
                : Host.SettingsLoad.UsedBackup ? $"已从备份恢复设置。{load}" : load;
        }
        finally { _syncing = false; }
    }
    public void RefreshStatus(string status)
    {
        _status.Text = status;
        _retry.Enabled = _retry.Visible = Host.SettingsSaveState == SettingsSaveState.Failed;
    }
    private void RefreshDisplaysIfNeeded()
    {
        var expected = new[] { (string?)null }.Concat(Host.Displays.Select(d => (string?)d.Id.Value)).ToArray();
        if (!expected.SequenceEqual(_display.Items.Cast<DisplayChoice>().Select(c => c.Id), StringComparer.OrdinalIgnoreCase))
        {
            _display.Items.Clear(); _display.Items.Add(new DisplayChoice(null, "跟随系统"));
            foreach (var display in Host.Displays)
                _display.Items.Add(new DisplayChoice(display.Id.Value, $"{display.Id.Value} {(display.IsPrimary ? "（主显示器）" : "")}"));
        }
        for (var i = 0; i < _display.Items.Count; i++)
            if (_display.Items[i] is DisplayChoice c && string.Equals(c.Id, Host.PreferredDisplayId, StringComparison.OrdinalIgnoreCase))
            { _display.SelectedIndex = i; return; }
        if (_display.Items.Count > 0) _display.SelectedIndex = 0;
    }
    private sealed record DisplayChoice(string? Id, string Label) { public override string ToString() => Label; }
}
