using YimyWalk.App.Host;
using YimyWalk.App.Settings;
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
        AddHeading(layout, "设置", "这里设置所有桌宠的默认值。");
        var card = UiTheme.CreateCard();
        card.Dock = DockStyle.Top;
        card.Height = 264;
        var cardTitle = new Label { Text = "显示与行为", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top };
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        AddGridRow(grid, "桌宠显示缩放", _scale, "0.1–8.0，修改后立即生效。");
        AddGridRow(grid, "新桌宠出现的位置", _display, "选择桌宠出现的显示器。");
        AddGridRow(grid, "允许落到可见窗口顶部", _landing, "桌宠会停在窗口上边缘并沿边行走。");
        card.Controls.Add(grid);
        card.Controls.Add(cardTitle);
        AddRow(layout, card);

        var saveCard = UiTheme.CreateCard();
        saveCard.Dock = DockStyle.Top;
        saveCard.Height = 142;
        var saveTitle = new Label { Text = "保存状态", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top };
        _status.Dock = DockStyle.Top;
        _diagnostic.Dock = DockStyle.Top;
        _retry.Dock = DockStyle.Top;
        _retry.Margin = new Padding(0, UiTheme.Space8, 0, 0);
        _retry.Click += (_, _) => Host.RetrySettingsSave();
        saveCard.Controls.Add(_retry);
        saveCard.Controls.Add(_diagnostic);
        saveCard.Controls.Add(_status);
        saveCard.Controls.Add(saveTitle);
        AddRow(layout, saveCard);

        _scale.ValueChanged += (_, _) => { if (!_syncing) Host.GlobalScale = (float)_scale.Value; };
        _display.SelectedIndexChanged += (_, _) =>
        {
            if (_syncing || _display.SelectedValue is not DisplayChoice choice) return;
            Host.SetPreferredDisplay(choice.Id);
        };
        _landing.CheckedChanged += (_, _) => { if (!_syncing) Host.WindowLandingEnabled = _landing.Checked; };
    }

    public override string PageTitle => "设置";

    public override void RefreshFromHost()
    {
        _syncing = true;
        try
        {
            _scale.Value = (decimal)Host.GlobalScale;
            _landing.Checked = Host.WindowLandingEnabled;
            RefreshDisplaysIfNeeded();
            _status.Text = $"设置：{UiText.SaveStateLabel(Host.SettingsSaveState)}";
            _retry.Enabled = Host.SettingsSaveState == SettingsSaveState.Failed;
            _diagnostic.Text = Host.SettingsSaveError is { Length: > 0 } error
                ? $"保存错误：{error}"
                : Host.SettingsLoad.Diagnostic ?? string.Empty;
        }
        finally { _syncing = false; }
    }

    public void RefreshStatus(string status)
    {
        _status.Text = status;
        _retry.Enabled = Host.SettingsSaveState == SettingsSaveState.Failed;
    }

    private void RefreshDisplaysIfNeeded()
    {
        var expected = new[] { (string?)null }.Concat(Host.Displays.Select(display => (string?)display.Id.Value)).ToArray();
        var current = _display.Items.Cast<DisplayChoice>().Select(choice => choice.Id).ToArray();
        if (expected.SequenceEqual(current, StringComparer.OrdinalIgnoreCase))
        {
            SelectDisplay();
            return;
        }

        _display.Items.Clear();
        _display.Items.Add(new DisplayChoice(null, "跟随系统"));
        foreach (var display in Host.Displays)
            _display.Items.Add(new DisplayChoice(display.Id.Value, DisplayLabel(display)));
        SelectDisplay();
    }

    private void SelectDisplay()
    {
        var selected = Host.PreferredDisplayId;
        for (var index = 0; index < _display.Items.Count; index++)
            if (_display.Items[index] is DisplayChoice choice && string.Equals(choice.Id, selected, StringComparison.OrdinalIgnoreCase))
            {
                _display.SelectedIndex = index;
                return;
            }
        if (_display.Items.Count > 0) _display.SelectedIndex = 0;
    }

    private static void AddGridRow(TableLayoutPanel grid, string label, Control editor, string help)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(UiTheme.CreateBodyLabel(label), 0, row);
        grid.Controls.Add(UiTheme.WrapSwitch(editor), 1, row);
        var helpLabel = UiTheme.CreateMutedLabel(help);
        grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(helpLabel, 1, row + 1);
    }

    private static string DisplayLabel(DisplayInfo display)
        => $"{display.Id.Value} {(display.IsPrimary ? "（主显示器）" : string.Empty)}";

    private sealed record DisplayChoice(string? Id, string Label)
    {
        public override string ToString() => Label;
    }
}
