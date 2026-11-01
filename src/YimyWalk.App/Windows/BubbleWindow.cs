using YimyWalk.App.Rendering;

namespace YimyWalk.App.Windows;

internal sealed class BubbleWindow : Form
{
    private readonly string _text;
    private BubbleArtwork? _artwork;
    private int _renderedDpi;
    private Size _available;
    private Rectangle? _petBounds;

    public BubbleWindow(string text, Point anchor)
    {
        _text = text;
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Location = anchor;
        try { Rebuild(DeviceDpi, Screen.FromPoint(anchor).WorkingArea.Size); }
        catch { Dispose(); throw; }
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE
                | NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT;
            return parameters;
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WM_MOUSEACTIVATE || message.Msg == NativeMethods.WM_NCHITTEST)
        {
            message.Result = message.Msg == NativeMethods.WM_MOUSEACTIVATE
                ? (IntPtr)NativeMethods.MA_NOACTIVATE
                : (IntPtr)NativeMethods.HTTRANSPARENT;
            return;
        }
        base.WndProc(ref message);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Rebuild(DeviceDpi, Screen.FromRectangle(_petBounds ?? Bounds).WorkingArea.Size);
        Present();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        Rebuild(e.DeviceDpiNew, Screen.FromRectangle(_petBounds ?? Bounds).WorkingArea.Size);
        if (_petBounds is { } pet) PositionAbove(pet, e.DeviceDpiNew);
        Present();
    }

    public void MoveAbove(Rectangle petBounds, int dpi)
    {
        _petBounds = petBounds;
        var changed = Rebuild(dpi, Screen.FromRectangle(petBounds).WorkingArea.Size);
        PositionAbove(petBounds, dpi);
        if (changed) Present();
    }

    private bool Rebuild(int dpi, Size available)
    {
        if (_artwork is not null && dpi == _renderedDpi && available == _available) return false;
        var next = BubbleRenderer.Render(_text, dpi, available);
        var previous = _artwork;
        _artwork = next;
        _renderedDpi = dpi;
        _available = available;
        ClientSize = next.Pixels.Size;
        previous?.Dispose();
        return true;
    }

    private void PositionAbove(Rectangle petBounds, int dpi)
        => Location = BubbleRenderer.Place(petBounds, Size, Screen.FromRectangle(petBounds).WorkingArea, dpi);

    private void Present()
    {
        if (IsHandleCreated && !IsDisposed && _artwork is not null)
            LayeredWindowRenderer.Present(Handle, _artwork.Pixels, Location);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _artwork?.Dispose(); _artwork = null; }
        base.Dispose(disposing);
    }
}
