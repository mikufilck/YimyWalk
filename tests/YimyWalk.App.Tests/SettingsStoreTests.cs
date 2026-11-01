using YimyWalk.App.Settings;
using YimyWalk.Core.Settings;

namespace YimyWalk.App.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "YimyWalkAppTests", Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecoveredBackupSurvivesSaveAndASecondPrimaryCorruption(bool useFallback)
    {
        var store = useFallback
            ? new SettingsStore(_root, (_, _, _) => throw new PlatformNotSupportedException())
            : new SettingsStore(_root);
        var expected = SettingsDocument.Defaults with { Scale = 2 };
        store.Save(expected);
        store.Save(expected with { Scale = 3 });
        File.WriteAllText(store.SettingsPath, "broken");
        var recovered = store.Load();
        Assert.True(recovered.UsedBackup);
        store.Save(recovered.Settings);
        File.WriteAllText(store.SettingsPath, "broken-again");
        var second = store.Load();
        Assert.True(second.UsedBackup);
        Assert.Equal(expected, second.Settings);
    }

    [Fact]
    public void ValidPrimaryRotatesIntoBackupAndInvalidDocumentDoesNotTouchFiles()
    {
        var store = new SettingsStore(_root);
        store.Save(SettingsDocument.Defaults with { Scale = 2 });
        store.Save(SettingsDocument.Defaults with { Scale = 3 });
        var primary = File.ReadAllBytes(store.SettingsPath);
        var backup = File.ReadAllBytes(store.BackupPath);
        Assert.Throws<ArgumentException>(() => store.Save(SettingsDocument.Defaults with { Scale = -1 }));
        Assert.Equal(primary, File.ReadAllBytes(store.SettingsPath));
        Assert.Equal(backup, File.ReadAllBytes(store.BackupPath));
        Assert.False(File.Exists(store.TemporaryPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
