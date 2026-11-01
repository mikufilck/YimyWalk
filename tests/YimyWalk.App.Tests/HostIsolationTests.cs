using System.Reflection;
using System.Runtime.ExceptionServices;
using YimyWalk.App.Host;
using YimyWalk.Core.Imaging;

namespace YimyWalk.App.Tests;

public sealed class HostIsolationTests
{
    [Fact]
    public void HostSavesOnlyToItsInjectedDirectoryAndStopsFailedCreationBatches()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "YimyWalkHostTest-" + Guid.NewGuid().ToString("N"));
            try
            {
                var role = Path.Combine(root, "characters", "slime");
                Directory.CreateDirectory(role);
                using (var png = new PngImage(96, 96, new Rgba32(40, 180, 80))) png.SaveAsPng(Path.Combine(role, "sprite.png"));
                var settingsRoot = Path.Combine(root, "settings");
                using (var host = new AppHost(settingsRoot))
                {
                    host.CharacterRootOverride = Path.Combine(root, "characters");
                    host.Start();
                    host.GlobalScale = 1.4f;
                    host.RetrySettingsSave();
                    Assert.Equal(1.4f, host.Settings.Scale);
                    Assert.True(File.Exists(Path.Combine(settingsRoot, "settings.json")));
                    for (var i = 0; i < 33; i++) host.SpawnVirtualFood();
                    Assert.Equal(32, host.VirtualFoodCount);
                    Assert.Contains("上限", host.RuntimeDiagnostic);
                    var errors = 0;
                    host.Batch.Failed += _ => errors++;
                    host.Batch.Enqueue(() => throw new InvalidOperationException("injected failure"), 100);
                    var drain = typeof(BatchCreationQueue).GetMethod("DrainBatch", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    drain.Invoke(host.Batch, new object?[] { null, EventArgs.Empty });
                    drain.Invoke(host.Batch, new object?[] { null, EventArgs.Empty });
                    Assert.Equal(1, errors);
                    Assert.Equal(0, host.PendingCreationCount);
                    Assert.NotNull(host.RuntimeDiagnostic);
                    Assert.True(File.Exists(Path.Combine(settingsRoot, "logs", "runtime.log")));
                }
            }
            catch (Exception error) { failure = error; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "Host test thread must terminate");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
