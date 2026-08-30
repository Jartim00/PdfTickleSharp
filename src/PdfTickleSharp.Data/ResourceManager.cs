using System.Buffers.Binary;
using System.IO.Compression;
using System.Reflection;

namespace PdfTickleSharp.Data;

/// <summary>
/// Manages embedded resources for PDF generation, including the bundled fonts.
/// </summary>
public static class ResourceManager
{
    /// <summary>Folders inside the assembly whose contents are exposed as resources.</summary>
    private static readonly string[] ResourceFolders = [".Fonts.", ".Resources."];

    private static readonly Dictionary<string, byte[]> _embeddedResources = new();
    private static readonly object _syncRoot = new();
    private static bool _initialized;

    /// <summary>
    /// Initializes the resource manager and loads embedded resources.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        lock (_syncRoot)
        {
            if (_initialized) return;
            LoadEmbeddedResources();
            _initialized = true;
        }
    }

    /// <summary>
    /// Gets an embedded resource by name.
    /// </summary>
    /// <param name="resourceName">The name of the resource to retrieve, without its extension.</param>
    /// <returns>The resource data as bytes, or null if not found.</returns>
    public static byte[]? GetResource(string resourceName)
    {
        if (!_initialized) Initialize();
        return _embeddedResources.GetValueOrDefault(resourceName);
    }

    /// <summary>
    /// Gets a list of available embedded resource names.
    /// </summary>
    /// <returns>An array of available resource names.</returns>
    public static string[] GetAvailableResources()
    {
        if (!_initialized) Initialize();
        return _embeddedResources.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Gets a default test image for PDF generation.
    /// </summary>
    /// <returns>A small PNG image as bytes.</returns>
    public static byte[] GetDefaultTestImage() => CreateGradientImage(64, 64);

    /// <summary>
    /// Gets a solid-colour test image for PDF generation.
    /// </summary>
    /// <param name="red">Red component (0-255).</param>
    /// <param name="green">Green component (0-255).</param>
    /// <param name="blue">Blue component (0-255).</param>
    /// <returns>A colored PNG image as bytes.</returns>
    public static byte[] GetColoredTestImage(byte red, byte green, byte blue)
    {
        const int size = 32;
        var pixels = new byte[size * size * 3];

        for (var i = 0; i < size * size; i++)
        {
            pixels[i * 3] = red;
            pixels[i * 3 + 1] = green;
            pixels[i * 3 + 2] = blue;
        }
        return EncodePng(pixels, size, size);
    }

    /// <summary>
    /// Creates a two-axis colour gradient with a border, so that scaling and
    /// orientation problems are visible when the image is placed in a PDF.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>A PNG image as bytes.</returns>
    public static byte[] CreateGradientImage(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        var pixels = new byte[width * height * 3];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 3;
                var onBorder = x < 2 || y < 2 || x >= width - 2 || y >= height - 2;

                if (onBorder)
                {
                    pixels[offset] = 20;
                    pixels[offset + 1] = 20;
                    pixels[offset + 2] = 20;
                }
                else
                {
                    pixels[offset] = (byte)(255 * x / Math.Max(1, width - 1));
                    pixels[offset + 1] = (byte)(255 * y / Math.Max(1, height - 1));
                    pixels[offset + 2] = 160;
                }
            }
        }
        return EncodePng(pixels, width, height);
    }

    /// <summary>
    /// Creates a PNG with an alpha channel: an opaque disc that fades to fully
    /// transparent at the edges, so soft-mask handling can be verified.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>An RGBA PNG image as bytes.</returns>
    public static byte[] CreateTransparentTestImage(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        var pixels = new byte[width * height * 4];
        var centreX = (width - 1) / 2.0;
        var centreY = (height - 1) / 2.0;
        var radius = Math.Min(centreX, centreY);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                var distance = Math.Sqrt((x - centreX) * (x - centreX) + (y - centreY) * (y - centreY));

                pixels[offset] = 220;
                pixels[offset + 1] = (byte)(40 + 160 * y / Math.Max(1, height - 1));
                pixels[offset + 2] = 60;

                // Opaque in the middle, fading over the outermost quarter of the radius.
                var fade = (radius - distance) / (radius * 0.25);
                pixels[offset + 3] = (byte)Math.Clamp(fade * 255, 0, 255);
            }
        }
        return EncodePng(pixels, width, height, hasAlpha: true);
    }

    /// <summary>
    /// Encodes 8-bit RGB pixel data as an uncompressed-filter PNG file.
    /// </summary>
    /// <param name="rgb">Pixel data in row-major order, three bytes per pixel, or four when <paramref name="hasAlpha"/> is set.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="hasAlpha">True when the data carries an alpha channel.</param>
    /// <returns>A complete PNG file.</returns>
    public static byte[] EncodePng(byte[] rgb, int width, int height, bool hasAlpha = false)
    {
        ArgumentNullException.ThrowIfNull(rgb);

        var bytesPerPixel = hasAlpha ? 4 : 3;
        var rowLength = width * bytesPerPixel;

        // Each scanline is prefixed with filter type 0 (None).
        var raw = new byte[height * (rowLength + 1)];
        for (var y = 0; y < height; y++)
        {
            var source = y * rowLength;
            var target = y * (rowLength + 1);
            raw[target] = 0;
            Array.Copy(rgb, source, raw, target + 1, rowLength);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(raw, 0, raw.Length);
        }

        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), (uint)height);
        header[8] = 8;  // bit depth
        header[9] = (byte)(hasAlpha ? 6 : 2); // colour type: truecolour, with alpha when requested
        header[10] = 0; // deflate
        header[11] = 0; // adaptive filtering
        header[12] = 0; // no interlacing

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var length = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);

        var checksummed = new byte[typeBytes.Length + data.Length];
        typeBytes.CopyTo(checksummed, 0);
        data.CopyTo(checksummed, typeBytes.Length);

        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(checksummed));

        stream.Write(length);
        stream.Write(checksummed);
        stream.Write(crc);
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
            }
            table[i] = value;
        }
        return table;
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFFu;
    }

    private static void LoadEmbeddedResources()
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!ResourceFolders.Any(folder => resourceName.Contains(folder, StringComparison.Ordinal))) continue;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            var resourceData = new byte[stream.Length];
            stream.ReadExactly(resourceData, 0, resourceData.Length);
            _embeddedResources[ExtractResourceKeyFromResourceName(resourceName)] = resourceData;
        }
    }

    private static string ExtractResourceKeyFromResourceName(string resourceName)
    {
        // Resource names look like "PdfTickleSharp.Data.Fonts.DejaVuSans-Bold.ttf";
        // the key is the file name without its extension.
        var parts = resourceName.Split('.');
        return parts.Length >= 2 ? parts[^2] : "Unknown";
    }
}
