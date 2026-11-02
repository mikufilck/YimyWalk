using YimyWalk.App.Host;
using YimyWalk.App.UI.Pages;
using YimyWalk.App.UI.Controls;
using YimyWalk.App.Windows;

namespace YimyWalk.App.UI;

/// <summary>
/// The on-demand tray manager. Pages are cached so navigation never resets their
/// selection, scroll position, or an editor that is currently being used.
/// </summary>
internal sealed class ManagerForm : Form
{
    private readonly AppHost _host;
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Cream };
    private readonly LinkLabel _saveError = new()
    {
        Text = "设置保存失败，点击查看并重试。", AccessibleName = "设置保存失败",
        Dock = DockStyle.Bottom, AutoSize = true, Visible = false,
        Font = UiTheme.BodyFont, LinkColor = UiTheme.NavInk, ActiveLinkColor = UiTheme.Ink,
        LinkBehavior = LinkBehavior.HoverUnderline, Padding = new Padding(28, 8, 28, 16)
    };
    private readonly Dictionary<ManagerPageId, ManagerPage> _pages = new();
    private readonly Dictionary<ManagerPageId, AntdUI.Button> _navigation = new();
    private readonly PictureBox _brandLogo = new()
    {
        Name = "BrandLogo",
        AccessibleName = "YimyWalk 标志",
        Height = 54,
        Dock = DockStyle.Bottom,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.Transparent,
        TabStop = false
    };
    private ManagerPageId _selectedPage;
    private bool _allowClose;
    private bool _refreshing;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 200 };

    public ManagerForm(AppHost host)
    {
        _host = host;
        UiTheme.ConfigureAntd();
        Text = "YimyWalk";
        Icon = AppIcon.Load();
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        ClientSize = new Size(Math.Min(LogicalToDeviceUnits(1120), area.Width - 48), Math.Min(LogicalToDeviceUnits(760), area.Height - 48));
        MinimumSize = new Size(Math.Min(LogicalToDeviceUnits(960), area.Width - 48), Math.Min(LogicalToDeviceUnits(640), area.Height - 48));
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = UiTheme.Cream;
        Padding = new Padding(8);
        DoubleBuffered = true;
        MaximizeBox = true;
        MinimizeBox = true;
        ShowInTaskbar = true;
        KeyPreview = true;

        BuildShell();
        _refreshTimer.Tick += OnRefreshTick;
        VisibleChanged += (_, _) => UpdateRefreshSchedule();
        Resize += (_, _) => UpdateRefreshSchedule();
        FormClosing += OnFormClosing;
    }

    public ManagerPageId SelectedPage => _selectedPage;

    private void BuildShell()
    {
        var shell = new TableLayoutPanel { Name = "ShellLayout", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(44)));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell); shell.Controls.Add(new ManagerChrome(this), 0, 0);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(UiTheme.SidebarWidth)));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(root, 0, 1);
        var sidebar = new Panel
        {
            Name = "Sidebar",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BackColor = UiTheme.Yellow,
            Padding = new Padding(18, 18, 18, 20)
        };
        root.Controls.Add(sidebar, 0, 0);

        var sidebarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(84)));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(60)));
        sidebar.Controls.Add(sidebarLayout);

        var brand = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        var wordmark = UiTheme.CreateBodyLabel("YimyWalk"); wordmark.Font = UiTheme.BrandFont;
        brand.Controls.Add(wordmark); brand.Controls.Add(UiTheme.CreateMutedLabel("咿米的桌面"));
        sidebarLayout.Controls.Add(brand, 0, 0);

        using (var stream = typeof(ManagerForm).Assembly.GetManifestResourceStream("YimyWalk.App.Resources.logo.png")
            ?? throw new InvalidOperationException("Application logo resource is missing."))
        using (var source = Image.FromStream(stream))
            _brandLogo.Image = new Bitmap(source);

        var nav = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        sidebarLayout.Controls.Add(nav, 0, 1);
        AddNavigation(nav, ManagerPageId.Settings, "设置");
        AddNavigation(nav, ManagerPageId.Interactions, "互动");
        AddNavigation(nav, ManagerPageId.Characters, "角色");
        AddNavigation(nav, ManagerPageId.Pets, "桌宠");
        AddNavigation(nav, ManagerPageId.Overview, "总览");

        var footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        sidebarLayout.Controls.Add(footer, 0, 2);
        footer.Controls.Add(_brandLogo);
        sidebar.SizeChanged += (_, _) =>
        {
            var room = sidebar.ClientSize.Height - sidebar.Padding.Vertical - LogicalToDeviceUnits(84) - 264;
            sidebarLayout.RowStyles[2].Height = Math.Clamp(room, 0, LogicalToDeviceUnits(60));
            footer.Visible = room > 54;
        };

        var content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Cream, Padding = Padding.Empty, Margin = Padding.Empty };
        root.Controls.Add(content, 1, 0);
        content.Controls.Add(_pageHost);
        content.Controls.Add(_saveError);
        _saveError.LinkClicked += (_, _) => SelectPage(ManagerPageId.Settings);

        SelectPage(ManagerPageId.Overview);
    }

    private void AddNavigation(Panel nav, ManagerPageId pageId, string label)
    {
        var button = UiTheme.CreateNavButton(label);
        button.Tag = pageId;
        button.IconSvg = UiMotifs.Icon(pageId switch { ManagerPageId.Overview => "home", ManagerPageId.Pets => "paw", ManagerPageId.Interactions => "heart", ManagerPageId.Settings => "settings", _ => "dog" });
        button.AccessibleName = $"导航到{label}";
        button.Click += (_, _) => SelectPage(pageId);
        nav.Controls.Add(button);
        _navigation.Add(pageId, button);
    }

    public void SelectPage(ManagerPageId pageId)
    {
        if (!_pages.TryGetValue(pageId, out var page))
        {
            page = CreatePage(pageId);
            _pages.Add(pageId, page);
        }

        _pageHost.SuspendLayout();
        try
        {
            if (page.Parent is null) _pageHost.Controls.Add(page);
            foreach (var cached in _pages.Values) cached.Visible = ReferenceEquals(cached, page);
            page.BringToFront();
            _selectedPage = pageId;
            foreach (var pair in _navigation)
            {
                pair.Value.DefaultBack = pair.Key == pageId ? Color.White : UiTheme.Sidebar;
                pair.Value.DefaultBorderColor = pair.Key == pageId ? UiTheme.Border : Color.Transparent;
                pair.Value.ForeColor = pair.Key == pageId ? UiTheme.NavInk : UiTheme.Ink;
                pair.Value.AccessibleDescription = pair.Key == pageId ? "当前页面" : string.Empty;
            }
            page.RefreshFromHost();
        }
        finally { _pageHost.ResumeLayout(true); }
        // Initial wrapping is measured before a new page receives its final
        // width. Recalculate the scroll extent after that layout has settled.
        page.PerformLayout();
    }

    private ManagerPage CreatePage(ManagerPageId pageId)
        => pageId switch
        {
            ManagerPageId.Overview => new OverviewPage(_host),
            ManagerPageId.Pets => new PetsPage(_host),
            ManagerPageId.Characters => new CharactersPage(_host),
            ManagerPageId.Interactions => new InteractionsPage(_host),
            ManagerPageId.Settings => new SettingsPage(_host),
            _ => throw new ArgumentOutOfRangeException(nameof(pageId))
        };

    public void RefreshCharacters()
    {
        if (_pages.TryGetValue(ManagerPageId.Characters, out var page) && page is CharactersPage characters)
            characters.RefreshCharacters();
        RefreshAllPages();
    }

    public void SetReloadStatus(int loadedCount, int failureCount)
    {
        if (_pages.TryGetValue(ManagerPageId.Characters, out var page) && page is CharactersPage characters)
            characters.SetReloadStatus(loadedCount, failureCount);
    }

    public void SetSettingsStatus(string status)
    {
        RefreshSaveFailure();
        if (_pages.TryGetValue(ManagerPageId.Settings, out var page) && page is SettingsPage settings)
            settings.RefreshStatus(status);
        RefreshAllPages();
    }

    public void UpdateCount() => RefreshAllPages();

    public void RefreshAllPages()
    {
        if (_refreshing || IsDisposed || _host.IsExiting || !Visible || WindowState == FormWindowState.Minimized) return;
        _refreshing = true;
        try
        {
            RefreshSaveFailure();
            if (_pages.TryGetValue(_selectedPage, out var page)) page.RefreshFromHost();
        }
        finally { _refreshing = false; }
    }

    private void RefreshSaveFailure()
        => _saveError.Visible = _host.SettingsSaveState == Settings.SettingsSaveState.Failed;

    private void OnRefreshTick(object? sender, EventArgs e) => RefreshAllPages();

    private void UpdateRefreshSchedule()
    {
        if (IsDisposed || _host.IsExiting) return;
        _refreshTimer.Enabled = Visible && WindowState != FormWindowState.Minimized;
        if (_refreshTimer.Enabled) RefreshAllPages();
    }

    public void StopRefreshing() => _refreshTimer.Stop();

    public void HideToTray()
    {
        _refreshTimer.Stop();
        Hide();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
        {
            _host.MarkExiting();
            _host.ExitThread();
            return;
        }
        if (_allowClose || _host.IsExiting)
        {
            _host.MarkExiting();
            return;
        }

        e.Cancel = true;
        _host.HideManagerToTray();
    }

    public void AllowClose() => _allowClose = true;

    internal void ToggleMaximize()
    {
        MaximizedBounds = Screen.FromControl(this).WorkingArea;
        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WM_NCHITTEST && WindowState == FormWindowState.Normal)
        {
            var p = PointToClient(new Point(unchecked((short)message.LParam.ToInt64()), unchecked((short)(message.LParam.ToInt64() >> 16))));
            var edge = LogicalToDeviceUnits(7);
            var left = p.X < edge; var right = p.X >= ClientSize.Width - edge;
            var top = p.Y < edge; var bottom = p.Y >= ClientSize.Height - edge;
            var hit = top && left ? 13 : top && right ? 14 : bottom && left ? 16 : bottom && right ? 17
                : left ? 10 : right ? 11 : top ? 12 : bottom ? 15 : 0;
            if (hit != 0) { message.Result = (IntPtr)hit; return; }
        }
        base.WndProc(ref message);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(UiTheme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= OnRefreshTick;
            _refreshTimer.Dispose();
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
            var logoImage = _brandLogo.Image;
            _brandLogo.Image = null;
            logoImage?.Dispose();
            Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
