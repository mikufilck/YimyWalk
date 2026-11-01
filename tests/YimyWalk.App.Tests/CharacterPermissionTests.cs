using System.Security.AccessControl;
using System.Security.Principal;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Imaging;

namespace YimyWalk.App.Tests;

public sealed class CharacterPermissionTests
{
    [Fact]
    public void ActualDeniedSpriteIsNormalizedAndOtherRolesStillRefresh()
    {
        var root = Path.Combine(Path.GetTempPath(), "YimyWalkAcl-" + Guid.NewGuid().ToString("N"));
        var role = Path.Combine(root, "denied");
        var good = Path.Combine(root, "good");
        Directory.CreateDirectory(role); Directory.CreateDirectory(good);
        var path = Path.Combine(role, "sprite.png");
        using (var png = new PngImage(96, 96, new Rgba32(40, 180, 80)))
        { png.SaveAsPng(path); png.SaveAsPng(Path.Combine(good, "sprite.png")); }
        var file = new FileInfo(path);
        var original = file.GetAccessControl();
        using var identity = WindowsIdentity.GetCurrent();
        using var catalog = new CharacterCatalog();
        try
        {
            var first = catalog.Scan(root)["denied"];
            var denied = file.GetAccessControl();
            denied.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.ReadData, AccessControlType.Deny));
            file.SetAccessControl(denied);
            var error = Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(role));
            Assert.IsType<UnauthorizedAccessException>(error.InnerException);
            catalog.Scan(root);
            Assert.Equal(2, catalog.Definitions.Count);
            Assert.Same(first, catalog.Definitions["denied"]);
            Assert.False(catalog.Definitions["good"].IsDisposed);
            Assert.Single(catalog.LastFailures);
        }
        finally
        {
            file.SetAccessControl(original);
            catalog.Dispose();
            Directory.Delete(root, true);
        }
    }
}
