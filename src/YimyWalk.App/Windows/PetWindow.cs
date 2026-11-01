using System.Numerics;
using YimyWalk.App.Rendering;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Windows;

internal sealed class PetWindow : Form
{
    private readonly PetEngine _engine;
    private readonly FrameSurfaceCache _cache;
    private readonly Host.DesktopWorldSurface? _desktopSurface;
    private float _globalScale;
    private RenderedSurface? _surface;
    private SurfaceLease? _surfaceLease;
    private RenderedSurface? _lastPresented;
    private Point _lastPresentedLocation;
    private float _renderedScale;
    private bool _renderedMirrored;
    private bool _visibleRequested = true;
    private bool _dragging;
    private Point _lastCursor;
    private double _lastCursorSeconds;
    private readonly Host.IMonotonicClock _clock;
    private Vector2 _dragVelocity;
    private Point _grabOffset;
    private bool _cacheDefinitionRetained;
    private float _dpiScale = 1f;
    public event Action<string>? Interaction;

    public PetWindow(PetInstance instance, PetSession session, FrameSurfaceCache cache, float globalScale = 1f, Host.IMonotonicClock? clock = null)
    {
        _clock = clock ?? Host.StopwatchClock.Instance;
        Instance = instance;
        _engine = new PetEngine(session.Surface, new Host.SystemRandomSource());
        _desktopSurface = session.Surface as Host.DesktopWorldSurface;
        _cache = cache;
        _cache.RetainDefinition(instance.Definition);
        _cacheDefinitionRetained = true;
        _globalScale = Math.Clamp(globalScale, 0.1f, 8f);
        FormBorderStyle = FormBorderStyle.None;
        // The window size is supplied in device pixels by the renderer. Dpi
        // scaling keeps WinForms from applying its default Font-based
        // AutoScale path each time a pet is created on another monitor.
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = false;
        BackColor = Color.Magenta;
        MouseDown += HandleMouseDown;
        MouseMove += HandleMouseMove;
        MouseUp += HandleMouseUp;
        MouseCaptureChanged += (_, _) => ReleaseDrag();
        Deactivate += (_, _) => ReleaseDrag();
        try { RefreshFrame(); }
        catch { Dispose(); throw; }
    }

    public PetInstance Instance { get; }

    public void SetGlobalScale(float scale)
    {
        var previousEffective = EffectiveScale;
        _globalScale = Math.Clamp(scale, 0.1f, 8f);
        // A custom instance replaces the global multiplier. Keep the new
        // global value cached so clearing the override uses it, but do not
        // resize this window while the override is active.
        if (Math.Abs(previousEffective - EffectiveScale) > 0.0001f)
            RefreshFrame();
    }

    protected override void Dispose(bool disposing)
    {
        _surfaceLease?.Dispose();
        _surfaceLease = null;
        if (_cacheDefinitionRetained)
        {
            _cacheDefinitionRetained = false;
            _cache.ReleaseDefinition(Instance.Definition);
        }

        base.Dispose(disposing);
    }

    public void SetVisibleWithoutActivation(bool visible)
    {
        _visibleRequested = visible;
        if (!IsHandleCreated)
        {
            if (visible) Show();
            else Hide();
            return;
        }

        NativeMethods.ShowWindow(Handle, visible ? NativeMethods.SW_SHOWNOACTIVATE : NativeMethods.SW_HIDE);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            return parameters;
        }
    }

    public void RefreshFrame()
    {
        if (IsDisposed || !_visibleRequested) return;
        if (_desktopSurface is not null)
        {
            var nextDpiScale = _desktopSurface.ScaleForPoint(Instance.FootPosition);
            if (float.IsFinite(nextDpiScale) && nextDpiScale > 0)
                _dpiScale = nextDpiScale;
        }
        var frame = Instance.Definition.SelectFrame(Instance.GetFrameKind(), Instance.AnimationTime);
        var scale = EffectiveScale;
        var x = (int)Math.Round(Instance.FootPosition.X - Instance.Definition.Anchor.X * scale);
        var y = (int)Math.Round(Instance.FootPosition.Y - Instance.Definition.Anchor.Y * scale);
        var location = new Point(x, y);
        if (_surface is not null && ReferenceEquals(_surface, _lastPresented)
            && _surface.FrameIndex == frame.Index && _renderedMirrored == frame.Mirrored
            && _renderedScale == scale && _lastPresentedLocation == location) return;
        var nextLease = _cache.Acquire(Instance.Definition, frame, frame.Mirrored, scale);
        var previousLease = _surfaceLease;
        _surfaceLease = nextLease;
        _surface = nextLease.Surface;
        _renderedScale = scale;
        _renderedMirrored = frame.Mirrored;
        previousLease?.Dispose();
        // Set the target monitor before creating/resizing the native handle so
        // WinForms can apply that monitor's DPI instead of the primary screen's.
        if (Location != location) Location = location;
        if (ClientSize != _surface.Bitmap.Size) ClientSize = _surface.Bitmap.Size;
        if (IsHandleCreated && (!ReferenceEquals(_lastPresented, _surface) || _lastPresentedLocation != location))
        {
            LayeredWindowRenderer.Present(Handle, _surface.Bitmap, location);
            _lastPresented = _surface;
            _lastPresentedLocation = location;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            m.Result = (IntPtr)NativeMethods.MA_NOACTIVATE;
            return;
        }
        if (m.Msg == NativeMethods.WM_NCHITTEST && _surface is not null)
        {
            // WM_NCHITTEST supplies the screen coordinate being queried in
            // lParam. Do not read the mutable global cursor here: Windows can
            // probe a point while the cursor is elsewhere (for example during
            // hit-test routing or on a negative-coordinate display).
            var screenPoint = new Point(
                unchecked((short)m.LParam.ToInt64()),
                unchecked((short)(m.LParam.ToInt64() >> 16)));
            var point = PointToClient(screenPoint);
            if (point.X < 0 || point.Y < 0 || point.X >= _surface.Bitmap.Width || point.Y >= _surface.Bitmap.Height || _surface.Alpha[point.Y * _surface.Bitmap.Width + point.X] < 12)
            {
                m.Result = (IntPtr)NativeMethods.HTTRANSPARENT;
                return;
            }
            m.Result = (IntPtr)NativeMethods.HTCLIENT;
            return;
        }
        base.WndProc(ref m);
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        Capture = true;
        _grabOffset = new Point(e.X, e.Y);
        _lastCursor = Cursor.Position;
        _lastCursorSeconds = _clock.Seconds;
        _engine.BeginDrag(Instance);
        Interaction?.Invoke("clicked");
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var cursor = Cursor.Position;
        var now = _clock.Seconds;
        var delta = (float)Math.Max(0.001, now - _lastCursorSeconds);
        var scale = EffectiveScale;
        var toss = new Vector2((cursor.X - _lastCursor.X) / delta, (cursor.Y - _lastCursor.Y) / delta);
        _lastCursor = cursor;
        _lastCursorSeconds = now;
        _engine.SetDraggedFootPosition(Instance, new Vector2(
            cursor.X - _grabOffset.X + Instance.Definition.Anchor.X * (float)scale,
            cursor.Y - _grabOffset.Y + Instance.Definition.Anchor.Y * (float)scale));
        _dragVelocity = toss;
    }

    private void HandleMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) ReleaseDrag();
    }

    private void ReleaseDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        Capture = false;
        _engine.ReleaseDrag(Instance, _dragVelocity);
        Interaction?.Invoke("dragged");
        _dragVelocity = Vector2.Zero;
    }

    private float EffectiveScale
        => PetLimits.EffectiveScale(Instance.Definition.CellWidth, Instance.Definition.CellHeight,
            Instance.Definition.Scale * Instance.ResolveScale(_globalScale) * _dpiScale);
}
