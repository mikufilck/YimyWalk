using System.Reflection;
using System.Runtime.InteropServices;
using YimyWalk.App.Host;
using YimyWalk.App.Windows;

namespace YimyWalk.App.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void AppExportsNoTypesAndImportsOnlyThroughNativeMethods()
    {
        var assembly = typeof(AppHost).Assembly;
        Assert.Empty(assembly.GetExportedTypes());
        foreach (var type in assembly.GetTypes())
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                if (method.GetCustomAttribute<DllImportAttribute>() is not null)
                    Assert.Equal(typeof(NativeMethods), type);
    }

    [Fact]
    public void RuntimeLogIsBoundedThrottledAndDoesNotRecordExceptionContents()
    {
        var directory = Path.Combine(Path.GetTempPath(), "YimyWalkAppLog-" + Guid.NewGuid().ToString("N"));
        var clock = new Clock();
        var log = new AppLog(directory, clock);
        try
        {
            for (var i = 0; i < 50; i++) log.Record("RENDER", new IOException("private file contents"));
            var path = Path.Combine(directory, "runtime.log");
            Assert.Single(File.ReadAllLines(path));
            Assert.DoesNotContain("private", File.ReadAllText(path));
            File.WriteAllBytes(path, new byte[AppLog.MaxFileBytes]);
            clock.Seconds = 61;
            log.Record("RENDER", new IOException());
            Assert.True(File.Exists(path + ".1"));
            Assert.True(new FileInfo(path).Length < AppLog.MaxFileBytes);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private sealed class Clock : IMonotonicClock { public double Seconds { get; set; } }
}
