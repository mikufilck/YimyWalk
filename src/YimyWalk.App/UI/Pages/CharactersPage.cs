using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI.Pages;

internal sealed class CharactersPage : ManagerPage
{
    private readonly ThemedListView _characters = new();
    private readonly Label _roleName = UiTheme.CreateBodyLabel();
    private readonly Label _roleId = UiTheme.CreateMutedLabel();
    private readonly Label _reloadStatus = UiTheme.CreateMutedLabel();
    private readonly CharacterFramePreview _preview;
    private readonly AntdUI.Button _refresh = UiTheme.CreateButton("刷新角色", icon: "refresh");
    private readonly AntdUI.Button _setDefault = UiTheme.CreateButton("设为默认角色", primary: true);
    private bool _synchronizing;

    public CharactersPage(AppHost host) : base(host)
    {
        _preview = new CharacterFramePreview(host.SurfaceCache) { Dock = DockStyle.Fill, MinimumSize = new Size(220, 220) };
        _characters.SetColumns(("名称", .4f), ("角色 ID", .4f), ("默认", .2f));

        var layout = CreateRootLayout();
        var heading = AddHeading(layout, "角色");
        _refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        heading.Controls.Add(_refresh);
        heading.SizeChanged += (_, _) => _refresh.Location = new Point(Math.Max(0, heading.Width - _refresh.Width), Math.Max(0, (heading.Height - _refresh.Height) / 2));

        var listCard = UiTheme.CreateCard();
        listCard.Controls.Add(_characters);

        var detailCard = UiTheme.CreateCard();
        detailCard.AutoScroll = true;
        var details = UiLayout.Stack();
        var actions = UiLayout.Flow();
        _refresh.Click += (_, _) => ReloadCharacters();
        _setDefault.Click += (_, _) =>
        {
            if (_characters.SelectedItems.Count == 0 || _characters.SelectedItems[0].Tag is not RoleChoice role) return;
            Host.SetSelectedRole(role.Id);
            _reloadStatus.Text = $"默认角色已设为 {role.DisplayName}。";
            RefreshCharacters();
            ShowSelectedRole();
        };
        actions.Controls.Add(_setDefault);
        var previewRow = new Panel { Dock = DockStyle.Top, Height = LogicalToDeviceUnits(240), Padding = new Padding(0, UiTheme.Space16, 0, UiTheme.Space16) };
        previewRow.Controls.Add(_preview);
        _roleName.Font = UiTheme.SectionFont;
        UiLayout.AddRow(details, _roleName);
        UiLayout.AddRow(details, _roleId);
        UiLayout.AddRow(details, previewRow);
        UiLayout.AddRow(details, actions);
        UiLayout.AddRow(details, _reloadStatus);
        detailCard.Controls.Add(details);
        UiLayout.ListDetail(layout, listCard, detailCard);
        details.SizeChanged += (_, _) =>
        {
            foreach (var label in new[] { _roleName, _roleId, _reloadStatus })
                label.MaximumSize = new Size(Math.Max(1, details.Width - 8), 0);
        };

        _characters.SelectedIndexChanged += (_, _) =>
        {
            if (_synchronizing) return;
            ShowSelectedRole();
        };
    }

    public override string PageTitle => "角色";

    public override void RefreshFromHost()
    {
        if (NeedsCharacterRefresh()) RefreshCharacters();
        ShowSelectedRole();
    }

    public void RefreshCharacters()
    {
        var previewed = (_characters.SelectedItems.Cast<ListViewItem>().FirstOrDefault()?.Tag as RoleChoice)?.Id;
        var topId = (_characters.TopItem?.Tag as RoleChoice)?.Id;
        _synchronizing = true;
        try
        {
            var current = Host.SelectedRoleId;
            _characters.BeginUpdate();
            try
            {
                _characters.Items.Clear();
                foreach (var definition in Host.Catalog.Definitions.Values
                    .OrderByDescending(definition => definition.Id.Equals(current, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var choice = new RoleChoice(definition.Id, UiText.CharacterName(definition.DisplayName));
                    var row = new ListViewItem(choice.DisplayName) { Tag = choice };
                    row.SubItems.Add(choice.Id);
                    row.SubItems.Add(choice.Id.Equals(current, StringComparison.OrdinalIgnoreCase) ? "当前" : string.Empty);
                    _characters.Items.Add(row);
                }
                var selected = FindRow(previewed) ?? FindRow(current);
                if (selected is not null) selected.Selected = true;
                else if (_characters.Items.Count > 0) _characters.Items[0].Selected = true;
            }
            finally { _characters.EndUpdate(); }
            if (FindRow(topId) is { } topRow) _characters.TopItem = topRow;
        }
        finally { _synchronizing = false; }
        ShowSelectedRole();
    }

    private ListViewItem? FindRow(string? id) => id is null ? null : _characters.Items.Cast<ListViewItem>()
        .FirstOrDefault(item => item.Tag is RoleChoice role && role.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void SetReloadStatus(int loadedCount, int failureCount)
        => _reloadStatus.Text = failureCount == 0
            ? $"已加载 {loadedCount} 个角色。"
            : $"已保留可用角色；{failureCount} 个刷新失败。";

    private void ReloadCharacters()
    {
        try
        {
            var failures = Host.ReloadCharacters();
            SetReloadStatus(Host.Catalog.Definitions.Count, failures.Count);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            _reloadStatus.Text = $"刷新失败：{exception.Message}";
        }
    }

    private void ShowSelectedRole()
    {
        var role = _characters.SelectedItems.Count > 0 ? _characters.SelectedItems[0].Tag as RoleChoice : null;
        if (role is null || !Host.Catalog.TryGet(role.Id, out var definition))
        {
            _roleName.Text = "没有可用角色";
            _roleId.Text = string.Empty;
            _setDefault.Enabled = false;
            _preview.ShowIdleFrame(null);
            return;
        }

        _roleName.Text = UiText.CharacterName(definition.DisplayName);
        _roleId.Text = $"角色 ID：{definition.Id}";
        var isDefault = definition.Id.Equals(Host.SelectedRoleId, StringComparison.OrdinalIgnoreCase);
        _setDefault.Text = isDefault ? "当前默认" : "设为默认角色";
        _setDefault.Enabled = !isDefault;
        _preview.ShowIdleFrame(definition);
    }

    private bool NeedsCharacterRefresh()
    {
        if (_characters.Items.Count != Host.Catalog.Definitions.Count) return true;
        return _characters.Items.Cast<ListViewItem>().Any(item => item.Tag is not RoleChoice role
            || !Host.Catalog.TryGet(role.Id, out var definition)
            || role.DisplayName != UiText.CharacterName(definition.DisplayName)
            || item.SubItems[2].Text != (role.Id.Equals(Host.SelectedRoleId, StringComparison.OrdinalIgnoreCase) ? "当前" : string.Empty));
    }

    private sealed record RoleChoice(string Id, string DisplayName);
}
