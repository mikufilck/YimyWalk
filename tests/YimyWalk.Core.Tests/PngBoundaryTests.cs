using YimyWalk.Core.Imaging;

namespace YimyWalk.Core.Tests;

public sealed class PngBoundaryTests
{
    [Fact]
    public void WriterRoundTripsColorAndAlphaAndRejectsCorruptChunks()
    {
        var directory = Path.Combine(Path.GetTempPath(), "YimyWalkPng-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "pixels.png");
            using var image = new PngImage(12, 9, new Rgba32(0, 0, 0, 0));
            image[2, 4] = new Rgba32(17, 93, 201, 128);
            image.SaveAsPng(path);
            using var decoded = PngImage.Load(path);
            for (var y = 0; y < image.Height; y++)
                for (var x = 0; x < image.Width; x++) Assert.Equal(image[x, y], decoded[x, y]);
            var bytes = File.ReadAllBytes(path);
            var original = bytes.ToArray();
            bytes[33 + 8] ^= 1;
            File.WriteAllBytes(path, bytes);
            Assert.Throws<InvalidDataException>(() => PngImage.Load(path));
            File.WriteAllBytes(path, original[..^12]);
            Assert.Throws<InvalidDataException>(() => PngImage.Load(path));
        }
        finally { Directory.Delete(directory, true); }
    }
}
