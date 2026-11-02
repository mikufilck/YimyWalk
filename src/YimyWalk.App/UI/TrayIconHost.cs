using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI;

internal sealed class TrayIconHost : IDisposable
{
    private readonly AppHost _host;
    private readonly NotifyIcon _icon;
    private readonly Icon _appIcon;
    private readonly ContextMenuStrip _menu;

    public TrayIconHost(AppHost host)
    {
        _host = host;
        var menu = _menu = new ContextMenuStrip
        {
            Renderer = new CompanionMenuRenderer(), BackColor = UiTheme.Cream, ForeColor = UiTheme.Ink,
            Font = UiTheme.BodyFont, ShowImageMargin = false, Padding = new Padding(8)
        };
        menu.Items.Add("打开管理器", null, (_, _) => _host.ShowManager());
        menu.Items.Add("清空桌宠", null, (_, _) => _host.ClearPets());
        menu.Items.Add("隐藏桌宠", null, (_, _) => _host.SetPetsVisible(false));
        menu.Items.Add("显示桌宠", null, (_, _) => _host.SetPetsVisible(true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出 YimyWalk", null, (_, _) => _host.ExitApplication());
        foreach (ToolStripItem item in menu.Items)
            if (item is ToolStripMenuItem) item.Padding = new Padding(14, 7, 14, 7);
        _appIcon = AppIcon.Load();
        _icon = new NotifyIcon { Icon = _appIcon, Text = "YimyWalk", ContextMenuStrip = menu, Visible = true };
        _icon.MouseClick += OnMouseClick;
    }

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) _host.ShowManager();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.MouseClick -= OnMouseClick;
        _icon.Dispose();
        _menu.Dispose();
        _appIcon.Dispose();
    }
}
