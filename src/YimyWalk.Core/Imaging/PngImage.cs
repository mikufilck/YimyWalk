using System.Buffers.Binary;
using System.IO.Compression;
using StbImageNext;

namespace YimyWalk.Core.Imaging;

public readonly record struct Rgba32(byte R, byte G, byte B, byte A = 255);

/// <summary>PNG-only managed image boundary. Runtime role validation happens before decoding.</summary>
public sealed class PngImage : IDisposable
{
    private byte[] _pixels;
    private readonly bool _hasAlpha;
    public int Width { get; }
    public int Height { get; }

    public PngImage(int width, int height, Rgba32 fill, bool hasAlpha = true)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        Width = width;
        Height = height;
        _hasAlpha = hasAlpha;
        _pixels = new byte[checked(width * height * 4)];
        for (var i = 0; i < _pixels.Length; i += 4)
        {
            _pixels[i] = fill.R; _pixels[i + 1] = fill.G; _pixels[i + 2] = fill.B; _pixels[i + 3] = fill.A;
        }
    }

    public Rgba32 this[int x, int y]
    {
        get { var i = Offset(x, y); return new(_pixels[i], _pixels[i + 1], _pixels[i + 2], _pixels[i + 3]); }
        set { var i = Offset(x, y); _pixels[i] = value.R; _pixels[i + 1] = value.G; _pixels[i + 2] = value.B; _pixels[i + 3] = value.A; }
    }

    private int Offset(int x, int y)
    {
        ObjectDisposedException.ThrowIf(_pixels.Length == 0, this);
        if ((uint)x >= Width || (uint)y >= Height) throw new ArgumentOutOfRangeException(nameof(x));
        return (y * Width + x) * 4;
    }

    public static PngImage Load(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        stream.ReadExactly(header);
        if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            throw new InvalidDataException("Expected PNG data");
        var width = BinaryPrimitives.ReadUInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadUInt32BigEndian(header[20..24]);
        if (width == 0 || height == 0 || width > 2048 || height > 2048)
            throw new InvalidDataException("PNG dimensions exceed the decoding budget");
        ValidateChunksAndInflation(stream, width, height);
        stream.Position = 0;
        try
        {
            var decoded = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            if (decoded.Width != width || decoded.Height != height) throw new InvalidDataException("PNG dimensions changed");
            var image = new PngImage(decoded.Width, decoded.Height, default);
            decoded.Data.AsSpan(0, image._pixels.Length).CopyTo(image._pixels);
            return image;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidDataException("Invalid PNG pixel data", ex);
        }
    }

    private static void ValidateChunksAndInflation(Stream stream, uint width, uint height)
    {
        if (stream.Length > 32L * 1024 * 1024) throw new InvalidDataException("PNG file exceeds the input budget");
        stream.Position = 8;
        using var compressed = new MemoryStream();
        var header = new byte[8];
        var checksum = new byte[4];
        var ended = false;
        while (stream.Position < stream.Length)
        {
            stream.ReadExactly(header);
            var length = BinaryPrimitives.ReadUInt32BigEndian(header);
            if (length > stream.Length - stream.Position - 4) throw new InvalidDataException("Truncated PNG chunk");
            var data = new byte[checked((int)length)];
            stream.ReadExactly(data);
            stream.ReadExactly(checksum);
            var crc = uint.MaxValue;
            foreach (var b in header.AsSpan(4)) crc = UpdateCrc(crc, b);
            foreach (var b in data) crc = UpdateCrc(crc, b);
            if (~crc != BinaryPrimitives.ReadUInt32BigEndian(checksum)) throw new InvalidDataException("Invalid PNG checksum");
            if (header.AsSpan(4).SequenceEqual("IDAT"u8)) compressed.Write(data);
            if (header.AsSpan(4).SequenceEqual("IEND"u8)) { ended = true; break; }
        }
        if (!ended || compressed.Length == 0) throw new InvalidDataException("PNG image data is missing");
        compressed.Position = 0;
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        var buffer = new byte[8192];
        // At most RGBA16 scanlines plus a generous Adam7 filter-byte allowance.
        var budget = (long)width * height * 8 + height * 7 + 65536;
        long total = 0;
        int count;
        while ((count = zlib.Read(buffer)) != 0)
            if ((total += count) > budget) throw new InvalidDataException("PNG inflation exceeds the pixel budget");
    }

    public void SaveAsPng(string path)
    {
        ObjectDisposedException.ThrowIf(_pixels.Length == 0, this);
        using var output = File.Create(path);
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), Height);
        header[8] = 8;
        header[9] = _hasAlpha ? (byte)6 : (byte)2;
        WriteChunk(output, "IHDR"u8, header);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (var y = 0; y < Height; y++)
            {
                zlib.WriteByte(0); // no row filter
                if (_hasAlpha) zlib.Write(_pixels.AsSpan(y * Width * 4, Width * 4));
                else for (var x = 0; x < Width; x++) zlib.Write(_pixels.AsSpan((y * Width + x) * 4, 3));
            }
        }
        WriteChunk(output, "IDAT"u8, compressed.ToArray());
        WriteChunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number); stream.Write(type); stream.Write(data);
        var crc = uint.MaxValue;
        foreach (var b in type) crc = UpdateCrc(crc, b);
        foreach (var b in data) crc = UpdateCrc(crc, b);
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        stream.Write(number);
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (var i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0u : 0xedb88320u);
        return crc;
    }

    public void Dispose() => _pixels = Array.Empty<byte>();
}
