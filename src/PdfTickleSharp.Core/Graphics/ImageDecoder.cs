using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace PdfTickleSharp.Core.Graphics;

/// <summary>
/// An image converted into the form a PDF image XObject expects.
/// </summary>
internal sealed class DecodedImage
{
    /// <summary>Gets the image width in pixels.</summary>
    public required int Width { get; init; }

    /// <summary>Gets the image height in pixels.</summary>
    public required int Height { get; init; }

    /// <summary>Gets the encoded sample data for the image stream.</summary>
    public required byte[] Data { get; init; }

    /// <summary>Gets the PDF filter the data is encoded with, such as /FlateDecode.</summary>
    public required string Filter { get; init; }

    /// <summary>Gets the PDF colour space name, such as /DeviceRGB.</summary>
    public required string ColorSpace { get; init; }

    /// <summary>Gets the number of bits per colour component.</summary>
    public int BitsPerComponent { get; init; } = 8;

    /// <summary>
    /// Gets the flate-compressed alpha channel used as a soft mask, or null when
    /// the image is fully opaque.
    /// </summary>
    public byte[]? SoftMask { get; init; }
}

/// <summary>
/// Decodes PNG and JPEG data into PDF image streams.
/// PNG is fully decoded and re-compressed because PDF cannot read the PNG
/// container; JPEG is passed through untouched because PDF reads JPEG natively.
/// </summary>
internal static class ImageDecoder
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Decodes image data, detecting the format from its header.
    /// </summary>
    /// <param name="data">The raw contents of a PNG or JPEG file.</param>
    /// <returns>The image in a form that can be embedded in a PDF.</returns>
    /// <exception cref="NotSupportedException">The format or variant is not supported.</exception>
    public static DecodedImage Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (IsPng(data)) return DecodePng(data);
        if (IsJpeg(data)) return DecodeJpeg(data);

        throw new NotSupportedException(
            "Unsupported image format. PdfTickleSharp supports PNG and JPEG images.");
    }

    private static bool IsPng(byte[] data)
    {
        if (data.Length < PngSignature.Length) return false;
        for (var i = 0; i < PngSignature.Length; i++)
        {
            if (data[i] != PngSignature[i]) return false;
        }
        return true;
    }

    private static bool IsJpeg(byte[] data) =>
        data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;

    // --- JPEG ------------------------------------------------------------

    /// <summary>
    /// Reads a JPEG's dimensions and component count from its frame header and
    /// wraps the original bytes for the PDF DCTDecode filter.
    /// </summary>
    private static DecodedImage DecodeJpeg(byte[] data)
    {
        var position = 2; // skip SOI

        while (position + 3 < data.Length)
        {
            if (data[position] != 0xFF) { position++; continue; }

            var marker = data[position + 1];
            position += 2;

            // Standalone markers carry no payload.
            if (marker is 0xD8 or 0x01 || (marker >= 0xD0 && marker <= 0xD7)) continue;
            if (marker == 0xD9) break; // EOI
            if (position + 1 >= data.Length) break;

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position, 2));

            // SOF0..SOF15 describe the frame; SOF4 (DHT), SOF8, SOF12 (DAC) do not.
            var isStartOfFrame = marker >= 0xC0 && marker <= 0xCF
                                 && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;

            if (isStartOfFrame)
            {
                if (position + 7 >= data.Length) break;

                var height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 5, 2));
                var components = data[position + 7];

                var colorSpace = components switch
                {
                    1 => "/DeviceGray",
                    3 => "/DeviceRGB",
                    4 => "/DeviceCMYK",
                    _ => throw new NotSupportedException($"JPEG with {components} components is not supported.")
                };

                return new DecodedImage
                {
                    Width = width,
                    Height = height,
                    Data = data,
                    Filter = "/DCTDecode",
                    ColorSpace = colorSpace,
                    BitsPerComponent = data[position + 2]
                };
            }

            position += segmentLength;
        }

        throw new NotSupportedException("JPEG data contains no frame header (SOF) and cannot be embedded.");
    }

    // --- PNG -------------------------------------------------------------

    /// <summary>
    /// Decodes a PNG: reads the header, inflates the pixel data, reverses the
    /// per-scanline filters, then separates colour from alpha.
    /// </summary>
    private static DecodedImage DecodePng(byte[] data)
    {
        var (header, pixelData, palette, transparency) = ReadPngChunks(data);

        if (header.Interlace != 0)
            throw new NotSupportedException("Interlaced (Adam7) PNG images are not supported.");

        var samplesPerPixel = header.ColorType switch
        {
            0 => 1, // grayscale
            2 => 3, // truecolour
            3 => 1, // palette index
            4 => 2, // grayscale + alpha
            6 => 4, // truecolour + alpha
            _ => throw new NotSupportedException($"PNG colour type {header.ColorType} is not supported.")
        };

        var raw = Inflate(pixelData);
        var samples = ReverseFilters(raw, header, samplesPerPixel);

        return BuildImage(header, samples, samplesPerPixel, palette, transparency);
    }

    private record PngHeader(int Width, int Height, byte BitDepth, byte ColorType, byte Interlace);

    private static (PngHeader Header, byte[] PixelData, byte[]? Palette, byte[]? Transparency)
        ReadPngChunks(byte[] data)
    {
        PngHeader? header = null;
        byte[]? palette = null;
        byte[]? transparency = null;
        var idat = new List<byte>();

        var position = PngSignature.Length;
        while (position + 8 <= data.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position, 4));
            var type = System.Text.Encoding.ASCII.GetString(data, position + 4, 4);
            var body = position + 8;

            if (length < 0 || body + length > data.Length) break;

            switch (type)
            {
                case "IHDR":
                    header = new PngHeader(
                        (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body, 4)),
                        (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 4, 4)),
                        data[body + 8],
                        data[body + 9],
                        data[body + 12]);
                    break;

                case "PLTE":
                    palette = data.AsSpan(body, length).ToArray();
                    break;

                case "tRNS":
                    transparency = data.AsSpan(body, length).ToArray();
                    break;

                case "IDAT":
                    idat.AddRange(data.AsSpan(body, length).ToArray());
                    break;

                case "IEND":
                    position = data.Length;
                    continue;
            }

            position = body + length + 4; // skip the chunk's CRC
        }

        if (header == null) throw new NotSupportedException("PNG data has no IHDR chunk.");
        if (idat.Count == 0) throw new NotSupportedException("PNG data has no IDAT chunk.");
        if (header.Width <= 0 || header.Height <= 0) throw new NotSupportedException("PNG has invalid dimensions.");

        return (header, idat.ToArray(), palette, transparency);
    }

    /// <summary>
    /// Reverses the PNG scanline filters, which encode each byte relative to its
    /// left, upper and upper-left neighbours.
    /// </summary>
    private static byte[] ReverseFilters(byte[] raw, PngHeader header, int samplesPerPixel)
    {
        var bitsPerPixel = samplesPerPixel * header.BitDepth;
        var bytesPerRow = (header.Width * bitsPerPixel + 7) / 8;
        var pixelStride = Math.Max(1, bitsPerPixel / 8);

        var output = new byte[bytesPerRow * header.Height];
        var previousRow = new byte[bytesPerRow];
        var position = 0;

        for (var row = 0; row < header.Height; row++)
        {
            if (position >= raw.Length)
                throw new NotSupportedException("PNG pixel data is truncated.");

            var filter = raw[position++];
            var available = Math.Min(bytesPerRow, raw.Length - position);
            if (available < bytesPerRow)
                throw new NotSupportedException("PNG pixel data is truncated.");

            var currentRow = new byte[bytesPerRow];
            Array.Copy(raw, position, currentRow, 0, bytesPerRow);
            position += bytesPerRow;

            for (var i = 0; i < bytesPerRow; i++)
            {
                int left = i >= pixelStride ? currentRow[i - pixelStride] : 0;
                int up = previousRow[i];
                int upperLeft = i >= pixelStride ? previousRow[i - pixelStride] : 0;

                currentRow[i] = filter switch
                {
                    0 => currentRow[i],
                    1 => (byte)(currentRow[i] + left),
                    2 => (byte)(currentRow[i] + up),
                    3 => (byte)(currentRow[i] + (left + up) / 2),
                    4 => (byte)(currentRow[i] + Paeth(left, up, upperLeft)),
                    _ => throw new NotSupportedException($"Unknown PNG filter type {filter}.")
                };
            }

            Array.Copy(currentRow, 0, output, row * bytesPerRow, bytesPerRow);
            previousRow = currentRow;
        }

        return output;
    }

    private static int Paeth(int left, int up, int upperLeft)
    {
        var estimate = left + up - upperLeft;
        var distanceLeft = Math.Abs(estimate - left);
        var distanceUp = Math.Abs(estimate - up);
        var distanceUpperLeft = Math.Abs(estimate - upperLeft);

        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpperLeft) return left;
        return distanceUp <= distanceUpperLeft ? up : upperLeft;
    }

    /// <summary>
    /// Turns decoded samples into 8-bit colour data plus an optional alpha mask,
    /// expanding palettes and narrow bit depths on the way.
    /// </summary>
    private static DecodedImage BuildImage(
        PngHeader header, byte[] samples, int samplesPerPixel, byte[]? palette, byte[]? transparency)
    {
        var pixelCount = header.Width * header.Height;
        var isColor = header.ColorType is 2 or 3 or 6;
        var componentsOut = isColor ? 3 : 1;

        var color = new byte[pixelCount * componentsOut];
        byte[]? alpha = null;

        var bytesPerRow = (header.Width * samplesPerPixel * header.BitDepth + 7) / 8;

        for (var y = 0; y < header.Height; y++)
        {
            for (var x = 0; x < header.Width; x++)
            {
                var pixel = y * header.Width + x;
                var target = pixel * componentsOut;

                switch (header.ColorType)
                {
                    case 0: // grayscale
                        color[target] = ReadSample(samples, bytesPerRow, y, x, 0, samplesPerPixel, header.BitDepth);
                        break;

                    case 2: // truecolour
                        for (var c = 0; c < 3; c++)
                            color[target + c] = ReadSample(samples, bytesPerRow, y, x, c, samplesPerPixel, header.BitDepth);
                        break;

                    case 3: // palette
                    {
                        if (palette == null) throw new NotSupportedException("Palette PNG has no PLTE chunk.");
                        int index = ReadRawSample(samples, bytesPerRow, y, x, 0, samplesPerPixel, header.BitDepth);

                        if (index * 3 + 2 < palette.Length)
                        {
                            color[target] = palette[index * 3];
                            color[target + 1] = palette[index * 3 + 1];
                            color[target + 2] = palette[index * 3 + 2];
                        }

                        if (transparency != null)
                        {
                            alpha ??= CreateOpaqueMask(pixelCount);
                            alpha[pixel] = index < transparency.Length ? transparency[index] : (byte)255;
                        }
                        break;
                    }

                    case 4: // grayscale + alpha
                        color[target] = ReadSample(samples, bytesPerRow, y, x, 0, samplesPerPixel, header.BitDepth);
                        alpha ??= CreateOpaqueMask(pixelCount);
                        alpha[pixel] = ReadSample(samples, bytesPerRow, y, x, 1, samplesPerPixel, header.BitDepth);
                        break;

                    case 6: // truecolour + alpha
                        for (var c = 0; c < 3; c++)
                            color[target + c] = ReadSample(samples, bytesPerRow, y, x, c, samplesPerPixel, header.BitDepth);
                        alpha ??= CreateOpaqueMask(pixelCount);
                        alpha[pixel] = ReadSample(samples, bytesPerRow, y, x, 3, samplesPerPixel, header.BitDepth);
                        break;
                }
            }
        }

        return new DecodedImage
        {
            Width = header.Width,
            Height = header.Height,
            Data = Deflate(color),
            Filter = "/FlateDecode",
            ColorSpace = isColor ? "/DeviceRGB" : "/DeviceGray",
            BitsPerComponent = 8,
            SoftMask = alpha == null ? null : Deflate(alpha)
        };
    }

    private static byte[] CreateOpaqueMask(int pixelCount)
    {
        var mask = new byte[pixelCount];
        Array.Fill(mask, (byte)255);
        return mask;
    }

    /// <summary>
    /// Reads one sample and scales it to the full 0-255 range.
    /// </summary>
    private static byte ReadSample(
        byte[] samples, int bytesPerRow, int y, int x, int component, int samplesPerPixel, int bitDepth)
    {
        var value = ReadRawSample(samples, bytesPerRow, y, x, component, samplesPerPixel, bitDepth);
        return bitDepth switch
        {
            16 => (byte)(value >> 8),
            8 => (byte)value,
            4 => (byte)(value * 17),        // 0..15  -> 0..255
            2 => (byte)(value * 85),        // 0..3   -> 0..255
            1 => (byte)(value * 255),       // 0..1   -> 0..255
            _ => (byte)value
        };
    }

    /// <summary>
    /// Reads one sample at its native bit depth, without rescaling.
    /// </summary>
    private static int ReadRawSample(
        byte[] samples, int bytesPerRow, int y, int x, int component, int samplesPerPixel, int bitDepth)
    {
        var sampleIndex = x * samplesPerPixel + component;

        if (bitDepth >= 8)
        {
            var bytesPerSample = bitDepth / 8;
            var offset = y * bytesPerRow + sampleIndex * bytesPerSample;
            if (offset + bytesPerSample > samples.Length) return 0;

            return bitDepth == 16
                ? (samples[offset] << 8) | samples[offset + 1]
                : samples[offset];
        }

        // Sub-byte depths pack several samples into each byte, most significant first.
        var bitPosition = sampleIndex * bitDepth;
        var byteOffset = y * bytesPerRow + bitPosition / 8;
        if (byteOffset >= samples.Length) return 0;

        var shift = 8 - bitDepth - bitPosition % 8;
        var mask = (1 << bitDepth) - 1;
        return (samples[byteOffset] >> shift) & mask;
    }

    // --- Compression -----------------------------------------------------

    /// <summary>
    /// Decompresses a zlib stream, as found in PNG IDAT chunks.
    /// </summary>
    private static byte[] Inflate(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Compresses data into the zlib stream that the PDF FlateDecode filter expects.
    /// </summary>
    public static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
}
