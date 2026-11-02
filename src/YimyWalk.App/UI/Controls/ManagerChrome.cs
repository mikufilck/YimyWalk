using YimyWalk.App.Windows;

namespace YimyWalk.App.UI.Controls;

internal sealed class ManagerChrome : Panel
{
    private readonly ToolTip _hints = new();
    internal ManagerChrome(ManagerForm owner)
    {
        Dock = DockStyle.Fill; BackColor = UiTheme.Cream;
        var caption = UiTheme.CreateMutedLabel("YimyWalk   /   咿米的桌面小窝");
        caption.Dock = DockStyle.Fill; caption.TextAlign = ContentAlignment.MiddleLeft;
        caption.AutoSize = false; caption.Padding = new Padding(18, 0, 0, 0);
        var controls = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 126, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = Padding.Empty };
        foreach (var (name, glyph, action) in new (string, string, Action)[]
        {
            ("最小化", "−", () => owner.WindowState = FormWindowState.Minimized),
            ("最大化或还原", "□", owner.ToggleMaximize),
            ("收起到托盘", "×", () => owner.Close())
        })
        {
            var button = new Button { Name = name, AccessibleName = name, Text = glyph, FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Cream, ForeColor = UiTheme.Muted, Font = UiTheme.SectionFont,
                Size = new Size(40, 34), Margin = new Padding(0, 4, 0, 0), TabStop = false };
            button.FlatAppearance.BorderSize = 0; button.FlatAppearance.MouseOverBackColor = UiTheme.Coral;
            _hints.SetToolTip(button, name);
            button.Click += (_, _) => action(); controls.Controls.Add(button);
        }
        void Drag(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (e.Clicks == 2) { owner.ToggleMaximize(); return; }
            NativeMethods.ReleaseCapture(); NativeMethods.SendMessage(owner.Handle, 0x00A1, (IntPtr)2, IntPtr.Zero);
        }
        MouseDown += Drag; caption.MouseDown += Drag;
        Controls.Add(caption); Controls.Add(controls);
    }
    protected override void Dispose(bool disposing) { if (disposing) _hints.Dispose(); base.Dispose(disposing); }
}
