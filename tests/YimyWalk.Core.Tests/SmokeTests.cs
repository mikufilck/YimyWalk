namespace YimyWalk.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void CoreAssemblyDoesNotReferenceWindowsForms()
    {
        var assembly = typeof(YimyWalk.Core.Characters.CharacterDefinition).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x =>
            x.Name is "System.Windows.Forms" or "WindowsBase" or "PresentationCore");
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name is "System.Drawing.Common");
        foreach (var type in assembly.GetTypes())
            foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                Assert.Null(System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Runtime.InteropServices.DllImportAttribute>(method));
    }
}
