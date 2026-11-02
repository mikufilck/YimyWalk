using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.UI.Pages;

internal sealed class PetsPage : ManagerPage
{
    private readonly PetListProjection _projection = new();
    private readonly ThemedListView _list = new();
    private readonly Label _empty = UiTheme.CreateMutedLabel("当前没有运行中的桌宠。");
    private readonly Label _emptyDetail = UiTheme.CreateBodyLabel("从左侧选择桌宠，调整它的设置。");
    private readonly Label _detailTitle = UiTheme.CreateBodyLabel();
    private readonly Label _detailMeta = UiTheme.CreateMutedLabel();
    private readonly Label _runtime = UiTheme.CreateBodyLabel();
    private readonly Label _operation = UiTheme.CreateMutedLabel();
    private readonly Label _walkSource = UiTheme.CreateMutedLabel();
    private readonly Label _dialogueSource = UiTheme.CreateMutedLabel();
    private readonly AntdUI.Select _walkOverride = UiTheme.CreateSelect(150);
    private readonly AntdUI.Select _dialogueOverride = UiTheme.CreateSelect(150);
    private readonly NumberOverrideEditor _activity = new(0.1m, 4m, 0.1m, 1);
    private readonly NumberOverrideEditor _scale = new(0.1m, 8m, 0.1m, 1);
    private readonly NumberOverrideEditor _jumpDesire = new(0m, 100m, 1m, 0, 100);
    private readonly NumberOverrideEditor _climbDesire = new(0m, 100m, 1m, 0, 100);
    private readonly NumberOverrideEditor _windowJumpDesire = new(0m, 100m, 1m, 0, 100);
    private readonly AntdUI.Button _remove = UiTheme.CreateButton("移除桌宠", icon: "trash", danger: true);
    private readonly AntdUI.Button _feed = UiTheme.CreateButton("喂食", primary: true, icon: "bone");
    private readonly AntdUI.Button _reset = UiTheme.CreateButton("恢复默认", icon: "reset");
    private readonly Panel _detailBody = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly TableLayoutPanel _footer = UiLayout.Stack();
    private readonly CharacterFramePreview _preview;
    private Guid? _editingId;
    private bool _syncing;
    private bool _bindingList;

    public PetsPage(AppHost host) : base(host)
    {
        _preview = new CharacterFramePreview(host.SurfaceCache) { Dock = DockStyle.Fill };
        _list.SetColumns(("编号", .36f), ("角色", .4f), ("状态", .24f));
        _walkOverride.Items.AddRange(new object[] { "跟随全局", "开启", "关闭" });
        _dialogueOverride.Items.AddRange(new object[] { "跟随全局", "开启", "关闭" });
        var layout = CreateRootLayout();
        AddHeading(layout, "桌宠");
        var listCard = UiTheme.CreateCard();
        listCard.Controls.Add(_list); listCard.Controls.Add(_empty);
        _empty.Dock = DockStyle.Bottom;
        UiLayout.ListDetail(layout, listCard, BuildDetailPanel());

        _list.SelectedIndexChanged += (_, _) => SelectFromList();
        _walkOverride.SelectedIndexChanged += (_, _) => ApplyBooleanOverride(isWalk: true);
        _dialogueOverride.SelectedIndexChanged += (_, _) => ApplyBooleanOverride(isWalk: false);
        _activity.ValueChanged += (_, _) => ApplyBehaviorChange(current => current with { ActivityRateOverride = _activity.OverrideValue });
        _jumpDesire.ValueChanged += (_, _) => ApplyBehaviorChange(current => current with { JumpDesireOverride = _jumpDesire.OverrideValue });
        _climbDesire.ValueChanged += (_, _) => ApplyBehaviorChange(current => current with { ClimbDesireOverride = _climbDesire.OverrideValue });
        _windowJumpDesire.ValueChanged += (_, _) => ApplyBehaviorChange(current => current with { WindowJumpDesireOverride = _windowJumpDesire.OverrideValue });
        _scale.ValueChanged += (_, _) =>
        {
            if (_syncing || _editingId is not { } id) return;
            Host.SetPetScaleOverride(id, _scale.OverrideValue);
            RefreshFromHost();
        };
        _remove.Click += (_, _) =>
        {
            if (_editingId is { } id) Host.RemovePet(id);
            RefreshFromHost();
        };
        _feed.Click += (_, _) =>
        {
            if (_editingId is not { } id) return;
            SetOperation(Host.FeedPet(id) ? "已喂食。" : "当前动作无法喂食。");
            RefreshFromHost();
        };
        _reset.Click += (_, _) =>
        {
            if (_editingId is not { } id) return;
            Host.ResetPetOverrides(id);
            RefreshFromHost();
            SetOperation("已恢复为全局设置。");
        };
    }

    public override string PageTitle => "桌宠";

    public override void RefreshFromHost()
    {
        if (_bindingList) return;
        _projection.Refresh(Host.CapturePets());
        ApplyProjectionToList();
        ShowSelectedPet();
    }

    private UiCard BuildDetailPanel()
    {
        var panel = UiTheme.CreateCard();
        var content = UiLayout.Stack();
        var summary = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = LogicalToDeviceUnits(118),
            ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Preview,
            Margin = new Padding(0, 0, 0, 14), Padding = new Padding(8)
        };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(110)));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        summary.Controls.Add(_preview, 0, 0);
        var identity = UiLayout.Stack(); identity.Margin = new Padding(12, 10, 0, 0);
        _detailTitle.Font = UiTheme.SectionFont;
        UiLayout.AddRow(identity, _detailTitle); UiLayout.AddRow(identity, _detailMeta); UiLayout.AddRow(identity, _runtime);
        identity.SizeChanged += (_, _) =>
        {
            foreach (var label in new[] { _detailTitle, _detailMeta, _runtime })
                label.MaximumSize = new Size(Math.Max(1, identity.ClientSize.Width), 0);
        };
        summary.Controls.Add(identity, 1, 0); UiLayout.AddRow(content, summary);
        var settings = new SettingRows(280);
        settings.Add("主动走动", "", _walkOverride, _walkSource);
        settings.Add("自动气泡", "", _dialogueOverride, _dialogueSource);
        settings.Add("桌宠缩放", "", _scale);
        settings.Add("活跃度倍率", "", _activity);
        settings.Add("跳跃倾向 (%)", "", _jumpDesire);
        settings.Add("攀爬倾向 (%)", "", _climbDesire);
        settings.Add("窗口跳跃倾向 (%)", "", _windowJumpDesire);
        UiLayout.AddRow(content, settings);
        var saveNote = UiTheme.CreateMutedLabel("单只桌宠的设置重启后恢复默认。");
        UiLayout.AddRow(content, saveNote);
        content.SizeChanged += (_, _) => saveNote.MaximumSize = new Size(Math.Max(1, content.Width - 8), 0);
        _detailBody.Controls.Add(content);
        var actions = UiLayout.Flow();
        actions.Padding = new Padding(0, 12, 0, 0);
        actions.Controls.Add(_feed); actions.Controls.Add(_reset); actions.Controls.Add(_remove);
        _operation.Visible = false;
        UiLayout.AddRow(_footer, _operation); UiLayout.AddRow(_footer, actions);
        _footer.Dock = DockStyle.Bottom;
        _emptyDetail.Dock = DockStyle.Top;
        panel.Controls.Add(_detailBody); panel.Controls.Add(_emptyDetail); panel.Controls.Add(_footer);
        return panel;
    }

    private void ApplyProjectionToList()
    {
        if (_bindingList) return;
        // Selection notifications are synchronous, including during native row removal.
        // BeginUpdate only stops painting; it does not suppress those notifications.
        _bindingList = true;
        var topId = _list.TopItem?.Tag is Guid top ? top : (Guid?)null;
        var oldOrder = _list.Items.Cast<ListViewItem>().Select(row => (Guid)row.Tag!).ToArray();
        _list.BeginUpdate();
        try
        {
            var byId = _list.Items.Cast<ListViewItem>().ToDictionary(item => (Guid)item.Tag!);
            var desired = _projection.Items.OrderBy(item => item.InstanceId).ToArray();
            var desiredIds = desired.Select(item => item.InstanceId).ToHashSet();
            foreach (var stale in byId.Keys.Where(id => !desiredIds.Contains(id)).ToArray())
                _list.Items.Remove(byId[stale]);
            for (var index = 0; index < desired.Length; index++)
            {
                var item = desired[index];
                if (!byId.TryGetValue(item.InstanceId, out var row))
                {
                    row = new ListViewItem(item.InstanceId.ToString("N")[..8]) { Tag = item.InstanceId };
                    row.SubItems.Add(UiText.CharacterName(item.RoleName));
                    row.SubItems.Add(UiText.PetStateLabel(item.State));
                    _list.Items.Insert(index, row);
                }
                else
                {
                    var name = UiText.CharacterName(item.RoleName);
                    if (row.SubItems[1].Text != name) row.SubItems[1].Text = name;
                    if (row.SubItems[2].Text != UiText.PetStateLabel(item.State)) row.SubItems[2].Text = UiText.PetStateLabel(item.State);
                }
            }
            _empty.Visible = desired.Length == 0;
            SelectRow(_projection.SelectedInstanceId);
        }
        finally
        {
            try
            {
                _list.EndUpdate();
                // Resolve the old top row or the nearest surviving neighbour by identity.
                if (topId is { } id && _list.Items.Count > 0)
                {
                    var live = _list.Items.Cast<ListViewItem>().ToDictionary(row => (Guid)row.Tag!);
                    var neighbours = oldOrder.Skip(Array.IndexOf(oldOrder, id))
                        .Concat(oldOrder.Take(Array.IndexOf(oldOrder, id)).Reverse());
                    var nextTop = neighbours.FirstOrDefault(live.ContainsKey);
                    if (live.TryGetValue(nextTop, out var topRow) && _list.TopItem != topRow) _list.TopItem = topRow;
                }
            }
            finally { _bindingList = false; }
        }
    }

    private void SelectFromList()
    {
        if (_syncing || _bindingList) return;
        var id = _list.SelectedItems.Count == 0 ? (Guid?)null : (Guid)_list.SelectedItems[0].Tag!;
        _projection.SetSelected(id);
        ShowSelectedPet();
    }

    private void SelectRow(Guid? id)
    {
        _syncing = true;
        try
        {
            foreach (ListViewItem item in _list.Items)
            {
                var selected = id is not null && (Guid)item.Tag! == id.Value;
                if (item.Selected != selected) item.Selected = selected;
            }
        }
        finally { _syncing = false; }
    }

    private void ShowSelectedPet()
    {
        var pet = _projection.Items.FirstOrDefault(item => item.InstanceId == _projection.SelectedInstanceId);
        var changedTarget = _editingId != pet?.InstanceId;
        _editingId = pet?.InstanceId;
        if (changedTarget) _operation.Visible = false;
        _detailBody.Visible = _footer.Visible = pet is not null;
        _emptyDetail.Visible = pet is null;
        if (pet is null) { _preview.ShowIdleFrame(null); return; }
        _detailTitle.Text = UiText.CharacterName(pet.Definition.DisplayName);
        _detailMeta.Text = $"编号 {pet.InstanceId.ToString("N")[..8]}";
        _runtime.Text = $"状态：{UiText.PetStateLabel(pet.State)} · 饥饿：{pet.Hunger:0.0}\n位置：{pet.Position.X:0}, {pet.Position.Y:0}";
        _preview.ShowIdleFrame(pet.Definition);
        _feed.Enabled = pet.State is PetState.Idle or PetState.Walk;
        _reset.Enabled = pet.ScaleOverride is not null || pet.BehaviorOverrides != PetBehaviorOverrides.Inherit;
        _syncing = true;
        try
        {
            if (changedTarget || !_walkOverride.ContainsFocus) _walkOverride.SelectedIndex = (int)pet.BehaviorOverrides.WalkOverride;
            if (changedTarget || !_dialogueOverride.ContainsFocus) _dialogueOverride.SelectedIndex = (int)pet.BehaviorOverrides.DialogueOverride;
            _scale.Bind(pet.InstanceId, pet.ScaleOverride, Host.GlobalScale);
            _activity.Bind(pet.InstanceId, pet.BehaviorOverrides.ActivityRateOverride, Host.ActivityRate);
            _jumpDesire.Bind(pet.InstanceId, pet.BehaviorOverrides.JumpDesireOverride, Host.JumpDesire);
            _climbDesire.Bind(pet.InstanceId, pet.BehaviorOverrides.ClimbDesireOverride, Host.ClimbDesire);
            _windowJumpDesire.Bind(pet.InstanceId, pet.BehaviorOverrides.WindowJumpDesireOverride, Host.WindowJumpDesire);
        }
        finally { _syncing = false; }
        _walkSource.Text = InstanceSettingState.WalkSource(pet.BehaviorOverrides, Host.DefaultWalkEnabled, Host.IsPaused);
        _dialogueSource.Text = InstanceSettingState.DialogueSource(pet.BehaviorOverrides, Host.DefaultDialogueEnabled, Host.DialogueEnabled);
        _walkSource.Visible = _walkSource.Text.Length > 0;
        _dialogueSource.Visible = _dialogueSource.Text.Length > 0;
    }

    private void ApplyBooleanOverride(bool isWalk)
    {
        var value = (isWalk ? _walkOverride : _dialogueOverride).SelectedIndex switch
        {
            1 => BooleanOverride.Enabled,
            2 => BooleanOverride.Disabled,
            _ => BooleanOverride.Inherit
        };
        ApplyBehaviorChange(current => isWalk ? current with { WalkOverride = value } : current with { DialogueOverride = value });
    }

    private void ApplyBehaviorChange(Func<PetBehaviorOverrides, PetBehaviorOverrides> change)
    {
        if (_syncing || _editingId is not { } id || Host.FindPet(id) is not { } pet) return;
        Host.SetPetBehaviorOverrides(id, change(pet.BehaviorOverrides));
        RefreshFromHost();
    }

    private void SetOperation(string message)
    {
        _operation.Text = message; _operation.Visible = true;
    }
}
