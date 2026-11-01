using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Rendering;

internal sealed class FrameSurfaceCache : IDisposable
{
    internal const long DefaultBudgetBytes = 64L * 1024 * 1024;
    private readonly Dictionary<SurfaceKey, Entry> _surfaces = new();
    private readonly Dictionary<CharacterDefinition, int> _definitionUsers = new();
    private readonly long _budget;
    private long _sequence;
    private bool _disposed;
    public FrameSurfaceCache() : this(DefaultBudgetBytes) { }
    public FrameSurfaceCache(long budgetBytes) => _budget = Math.Max(5, budgetBytes);
    public int Count => _surfaces.Count;
    public long AllocatedBytes { get; private set; }
    public long BudgetBytes => _budget;

    public void RetainDefinition(CharacterDefinition definition)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _definitionUsers.TryGetValue(definition, out var count);
        _definitionUsers[definition] = count + 1;
    }

    public void ReleaseDefinition(CharacterDefinition definition)
    {
        if (!_definitionUsers.TryGetValue(definition, out var count)) return;
        if (count > 1) { _definitionUsers[definition] = count - 1; return; }
        _definitionUsers.Remove(definition);
        foreach (var entry in _surfaces.Values.Where(entry => ReferenceEquals(entry.Key.Definition, definition)).ToArray()) Remove(entry);
    }

    public SurfaceLease Acquire(CharacterDefinition definition, CharacterFrame frame, bool mirrored, float scale = 1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        scale = PetLimits.EffectiveScale(frame.Width, frame.Height, scale);
        var key = new SurfaceKey(definition, frame.Index, mirrored,
            Math.Max(1, (int)Math.Round(frame.Width * scale)), Math.Max(1, (int)Math.Round(frame.Height * scale)));
        if (!_surfaces.TryGetValue(key, out var entry))
        {
            var bytes = checked((long)key.Width * key.Height * 5);
            while (AllocatedBytes + bytes > _budget)
            {
                var oldest = _surfaces.Values.Where(item => item.Users == 0).MinBy(item => item.LastUse);
                if (oldest is null) throw new RenderBudgetExceededException();
                Remove(oldest);
            }
            var surface = RenderedSurface.Create(definition, frame, mirrored, key.Width, key.Height);
            entry = new Entry(key, surface, bytes);
            _surfaces.Add(key, entry);
            AllocatedBytes += bytes;
        }
        entry.Users++;
        entry.LastUse = ++_sequence;
        return new SurfaceLease(entry.Surface, () =>
        {
            entry.Users--;
            if (entry.Users == 0 && !entry.Cached) Release(entry);
        });
    }

    private void Remove(Entry entry)
    {
        _surfaces.Remove(entry.Key);
        entry.Cached = false;
        if (entry.Users == 0) Release(entry);
    }

    private void Release(Entry entry)
    {
        entry.Surface.Dispose();
        AllocatedBytes -= entry.Bytes;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var entry in _surfaces.Values.ToArray()) Remove(entry);
        _definitionUsers.Clear();
    }

    private readonly record struct SurfaceKey(CharacterDefinition Definition, int FrameIndex, bool Mirrored, int Width, int Height);
    private sealed class Entry(SurfaceKey key, RenderedSurface surface, long bytes)
    {
        public SurfaceKey Key { get; } = key;
        public RenderedSurface Surface { get; } = surface;
        public long Bytes { get; } = bytes;
        public int Users { get; set; }
        public long LastUse { get; set; }
        public bool Cached { get; set; } = true;
    }
}

internal sealed class SurfaceLease(RenderedSurface surface, Action release) : IDisposable
{
    private Action? _release = release;
    public RenderedSurface Surface { get; } = surface;
    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}

internal sealed class RenderBudgetExceededException() : InvalidOperationException("The rendering memory budget is in use; the previous frame is retained.");

internal sealed class RenderedSurface : IDisposable
{
    private readonly DefinitionLease _owner;
    private readonly DefinitionLease.LeaseHandle _lease;
    private RenderedSurface(Bitmap bitmap, byte[] alpha, DefinitionLease owner, DefinitionLease.LeaseHandle lease)
    {
        Bitmap = bitmap; Alpha = alpha; _owner = owner; _lease = lease;
    }
    public Bitmap Bitmap { get; }
    public byte[] Alpha { get; }
    public int FrameIndex { get; private init; }
    public bool IsDisposed { get; private set; }

    public static RenderedSurface Create(CharacterDefinition definition, CharacterFrame frame, bool mirrored, int width, int height)
    {
        var owner = new DefinitionLease(definition);
        Bitmap? bitmap = null;
        DefinitionLease.LeaseHandle? lease = null;
        try
        {
            lease = owner.AcquireRenderer();
            bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            if (definition.Sampling == SpriteSampling.Smooth && (width != frame.Width || height != frame.Height))
            {
                using var source = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppPArgb);
                WriteNearest(source, frame, mirrored);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                using var attributes = new ImageAttributes();
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                // Spatial resampling of this frame only: no motion blur,
                // cross-frame blending or AI interpolation.
                graphics.DrawImage(source, new Rectangle(0, 0, width, height),
                    0, 0, frame.Width, frame.Height, GraphicsUnit.Pixel, attributes);
            }
            else WriteNearest(bitmap, frame, mirrored);
            var alpha = ReadAlpha(bitmap);
            return new RenderedSurface(bitmap, alpha, owner, lease) { FrameIndex = frame.Index };
        }
        catch { bitmap?.Dispose(); lease?.Dispose(); owner.Dispose(); throw; }
    }

    private static void WriteNearest(Bitmap bitmap, CharacterFrame frame, bool mirrored)
    {
        var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, bitmap.PixelFormat);
        try
        {
            var pixels = frame.RgbaPixels.Span;
            var row = new byte[bitmap.Width * 4];
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var sx = x * frame.Width / bitmap.Width;
                    if (mirrored) sx = frame.Width - 1 - sx;
                    var source = (y * frame.Height / bitmap.Height * frame.Width + sx) * 4;
                    var a = pixels[source + 3];
                    row[x * 4] = (byte)(pixels[source + 2] * a / 255);
                    row[x * 4 + 1] = (byte)(pixels[source + 1] * a / 255);
                    row[x * 4 + 2] = (byte)(pixels[source] * a / 255);
                    row[x * 4 + 3] = a;
                }
                Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * data.Stride), row.Length);
            }
        }
        finally { bitmap.UnlockBits(data); }
    }

    private static byte[] ReadAlpha(Bitmap bitmap)
    {
        var alpha = new byte[checked(bitmap.Width * bitmap.Height)];
        var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
        try
        {
            var row = new byte[bitmap.Width * 4];
            for (var y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                for (var x = 0; x < bitmap.Width; x++) alpha[y * bitmap.Width + x] = row[x * 4 + 3];
            }
        }
        finally { bitmap.UnlockBits(data); }
        return alpha;
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Bitmap.Dispose(); _lease.Dispose(); _owner.Dispose();
    }
}
