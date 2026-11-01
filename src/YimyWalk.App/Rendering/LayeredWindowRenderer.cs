using System.ComponentModel;
using System.Runtime.InteropServices;
using YimyWalk.App.Windows;

namespace YimyWalk.App.Rendering;

internal static class LayeredWindowRenderer
{
    public static void Present(IntPtr hwnd, Bitmap bitmap, Point screenLocation)
        => Present(hwnd, bitmap, screenLocation, NativeLayeredWindowApi.Instance);

    internal static void Present(IntPtr hwnd, Bitmap bitmap, Point screenLocation, ILayeredWindowApi api)
    {
        IntPtr screenDc = default, memoryDc = default, bitmapHandle = default, previous = default;
        try
        {
            screenDc = api.GetScreenDc();
            RequireHandle(screenDc, "GetDC");
            memoryDc = api.CreateMemoryDc(screenDc);
            RequireHandle(memoryDc, "CreateCompatibleDC");
            bitmapHandle = api.GetBitmap(bitmap);
            RequireHandle(bitmapHandle, "GetHbitmap");
            previous = api.SelectBitmap(memoryDc, bitmapHandle);
            RequireHandle(previous, "SelectObject");
            if (!api.Update(hwnd, screenDc, memoryDc, bitmap.Size, screenLocation))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateLayeredWindow failed");
        }
        finally
        {
            if (Valid(previous)) api.SelectBitmap(memoryDc, previous);
            if (Valid(bitmapHandle)) api.DeleteBitmap(bitmapHandle);
            if (Valid(memoryDc)) api.DeleteMemoryDc(memoryDc);
            if (Valid(screenDc)) api.ReleaseScreenDc(screenDc);
        }
    }

    private static bool Valid(IntPtr handle) => handle != IntPtr.Zero && handle != new IntPtr(-1);
    private static void RequireHandle(IntPtr handle, string operation)
    {
        if (!Valid(handle)) throw new Win32Exception(Marshal.GetLastWin32Error(), operation + " failed");
    }
}

internal interface ILayeredWindowApi
{
    IntPtr GetScreenDc();
    IntPtr CreateMemoryDc(IntPtr screenDc);
    IntPtr GetBitmap(Bitmap bitmap);
    IntPtr SelectBitmap(IntPtr dc, IntPtr bitmap);
    bool Update(IntPtr hwnd, IntPtr screenDc, IntPtr memoryDc, Size size, Point location);
    void DeleteBitmap(IntPtr bitmap);
    void DeleteMemoryDc(IntPtr dc);
    void ReleaseScreenDc(IntPtr dc);
}

internal sealed class NativeLayeredWindowApi : ILayeredWindowApi
{
    public static NativeLayeredWindowApi Instance { get; } = new();
    public IntPtr GetScreenDc() => NativeMethods.GetDC(IntPtr.Zero);
    public IntPtr CreateMemoryDc(IntPtr screenDc) => NativeMethods.CreateCompatibleDC(screenDc);
    public IntPtr GetBitmap(Bitmap bitmap) => bitmap.GetHbitmap(Color.FromArgb(0));
    public IntPtr SelectBitmap(IntPtr dc, IntPtr bitmap) => NativeMethods.SelectObject(dc, bitmap);
    public bool Update(IntPtr hwnd, IntPtr screenDc, IntPtr memoryDc, Size bitmapSize, Point location)
    {
        var destination = new NativeMethods.Point(location.X, location.Y);
        var source = new NativeMethods.Point(0, 0);
        var size = new NativeMethods.Size(bitmapSize.Width, bitmapSize.Height);
        var blend = new NativeMethods.BlendFunction { SourceConstantAlpha = 255, AlphaFormat = NativeMethods.AC_SRC_ALPHA };
        return NativeMethods.UpdateLayeredWindow(hwnd, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
    }
    public void DeleteBitmap(IntPtr bitmap) => NativeMethods.DeleteObject(bitmap);
    public void DeleteMemoryDc(IntPtr dc) => NativeMethods.DeleteDC(dc);
    public void ReleaseScreenDc(IntPtr dc) => NativeMethods.ReleaseDC(IntPtr.Zero, dc);
}
