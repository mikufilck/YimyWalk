using System.ComponentModel;
using YimyWalk.App.Rendering;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;

namespace YimyWalk.App.Tests;

public sealed class RenderingTests
{
    [Fact]
    public void ActualGetHbitmapFailureDoesNotLeakGdiObjects()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var disposed = new Bitmap(2, 2);
        disposed.Dispose();
        Assert.Throws<ArgumentException>(() => LayeredWindowRenderer.Present(IntPtr.Zero, disposed, Point.Empty));
        var baseline = TestNative.GetGuiResources(process.Handle, 0);
        for (var i = 0; i < 20; i++)
            Assert.Throws<ArgumentException>(() => LayeredWindowRenderer.Present(IntPtr.Zero, disposed, Point.Empty));
        Assert.Equal(baseline, TestNative.GetGuiResources(process.Handle, 0));
    }
    [Fact]
    public void CacheBudgetEvictsUnusedFramesAndKeepsPinnedPixelsAlive()
    {
        var definition = Definition();
        using var owner = new DefinitionLease(definition);
        using var cache = new FrameSurfaceCache(2 * 32 * 32 * 5);
        cache.RetainDefinition(definition);
        using var pinned = cache.Acquire(definition, definition.Frames[0], false);
        for (var i = 1; i < 9; i++)
        {
            using var next = cache.Acquire(definition, definition.Frames[i], false);
            Assert.InRange(cache.AllocatedBytes, 0, cache.BudgetBytes);
            Assert.False(pinned.Surface.IsDisposed);
        }
        cache.ReleaseDefinition(definition);
        Assert.Equal(0, cache.Count);
        Assert.False(pinned.Surface.IsDisposed);
        Assert.Equal(32, pinned.Surface.Bitmap.Width);
        pinned.Dispose();
        Assert.Equal(0, cache.AllocatedBytes);
    }

    [Fact]
    public void PinnedBudgetIsNotExceededAndCacheDisposalWaitsForHandles()
    {
        var definition = Definition();
        using var owner = new DefinitionLease(definition);
        using var cache = new FrameSurfaceCache(32 * 32 * 5);
        using var pinned = cache.Acquire(definition, definition.Frames[0], false);
        Assert.Throws<RenderBudgetExceededException>(() => cache.Acquire(definition, definition.Frames[1], false));
        cache.Dispose();
        Assert.False(pinned.Surface.IsDisposed);
        pinned.Dispose();
        Assert.True(pinned.Surface.IsDisposed);
        Assert.Equal(0, cache.AllocatedBytes);
    }

    [Fact]
    public void ExtremeScalingRespectsPixelsAndEquivalentSizesShareASurface()
    {
        var definition = Definition();
        using var owner = new DefinitionLease(definition);
        using var cache = new FrameSurfaceCache();
        using var first = cache.Acquire(definition, definition.Frames[0], false, 64);
        Assert.InRange(first.Surface.Bitmap.Width, 1, 1024);
        using var same = cache.Acquire(definition, definition.Frames[0], false, 63);
        Assert.Same(first.Surface, same.Surface);
    }

    [Theory]
    [InlineData(1, "")]
    [InlineData(2, "release-screen")]
    [InlineData(3, "delete-dc,release-screen")]
    [InlineData(4, "delete-bitmap,delete-dc,release-screen")]
    [InlineData(5, "restore,delete-bitmap,delete-dc,release-screen")]
    public void PresentationFailureCleansEveryPreviouslyAcquiredResource(int failure, string expected)
    {
        using var bitmap = new Bitmap(2, 2);
        var api = new FailingApi(failure);
        Assert.ThrowsAny<Exception>(() => LayeredWindowRenderer.Present(new IntPtr(7), bitmap, Point.Empty, api));
        Assert.Equal(expected, string.Join(',', api.Cleanup));
    }

    internal static CharacterDefinition Definition()
        => new()
        {
            Id = Guid.NewGuid().ToString(), DisplayName = "test", CellWidth = 32, CellHeight = 32,
            Scale = 1, Anchor = new Anchor(16, 29), Movement = new CharacterMovement(60, 450, 1500, 1500),
            Defaults = new CharacterDefaults(2, 8, 45), Dialogue = DialogueSet.Default,
            Frames = Enumerable.Range(0, 9).Select(index => new CharacterFrame(index, 32, 32,
                Enumerable.Repeat((byte)255, 32 * 32 * 4).ToArray(), FrameKind.Idle)).ToArray()
        };

    private sealed class FailingApi(int failure) : ILayeredWindowApi
    {
        public List<string> Cleanup { get; } = new();
        public IntPtr GetScreenDc() => failure == 1 ? IntPtr.Zero : new(1);
        public IntPtr CreateMemoryDc(IntPtr screenDc) => failure == 2 ? IntPtr.Zero : new(2);
        public IntPtr GetBitmap(Bitmap bitmap) => failure == 3 ? throw new InvalidOperationException() : new(3);
        public IntPtr SelectBitmap(IntPtr dc, IntPtr bitmap)
        {
            if (bitmap == new IntPtr(4)) { Cleanup.Add("restore"); return new(3); }
            return failure == 4 ? new(-1) : new(4);
        }
        public bool Update(IntPtr hwnd, IntPtr screenDc, IntPtr memoryDc, Size size, Point location) => failure != 5;
        public void DeleteBitmap(IntPtr bitmap) => Cleanup.Add("delete-bitmap");
        public void DeleteMemoryDc(IntPtr dc) => Cleanup.Add("delete-dc");
        public void ReleaseScreenDc(IntPtr dc) => Cleanup.Add("release-screen");
    }

    private static class TestNative
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern uint GetGuiResources(IntPtr process, uint flags);
    }
}
