using YimyWalk.App.Host;
using YimyWalk.App.Settings;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI.Pages;

internal sealed class SettingsPage : ManagerPage
{
    private readonly AntdUI.InputNumber _scale = UiTheme.CreateInputNumber(0.1m, 8m, 0.1m, 1, 120);
    private readonly AntdUI.Select _display = UiTheme.CreateSelect(300);
    private readonly AntdUI.Switch _landing = UiTheme.CreateSwitch();
    private readonly Label _status = UiTheme.CreateMutedLabel();
    private readonly Label _diagnostic = UiTheme.CreateMutedLabel();
    private readonly AntdUI.Button _retry = UiTheme.CreateButton("重试保存");
    private bool _syncing;

    public SettingsPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "设置");
        var display = new SettingRows(320);
        display.Add("桌宠缩放", "范围 0.1–8.0，修改后立即生效。", _scale);
        display.Add("默认显示器", "用于放置新创建的桌宠。", _display);
        display.Add("窗口顶部落脚", "允许桌宠停在可见窗口的上边缘。", _landing);
        AddRow(layout, display);
        _status.Margin = new Padding(0, 20, 0, 6);
        AddRow(layout, _status); AddRow(layout, _diagnostic); AddRow(layout, _retry);
        layout.SizeChanged += (_, _) =>
        {
            var width = Math.Max(1, layout.ClientSize.Width - 8);
            _status.MaximumSize = new Size(width, 0); _diagnostic.MaximumSize = new Size(width, 0);
        };
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
            _diagnostic.Text = Host.SettingsSaveError is { Length: > 0 } error ? $"保存失败：{error}"
                : firstRun || string.IsNullOrWhiteSpace(load) ? ""
                : Host.SettingsLoad.UsedBackup ? $"已从备份恢复设置。{load}" : load;
            _diagnostic.Visible = _diagnostic.Text.Length > 0;
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
