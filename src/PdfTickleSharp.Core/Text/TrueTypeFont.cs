using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfTickleSharp.Core.Text;

/// <summary>
/// Minimal TrueType font parser and subsetter.
/// Reads the tables PDF needs to embed a font as a CIDFontType2 descendant
/// (glyph metrics, the Unicode character map and the glyph outlines) and can
/// emit a reduced copy of the font containing only the glyphs a document uses.
/// </summary>
internal sealed class TrueTypeFont
{
    // Tables that must survive subsetting for a PDF FontFile2 stream.
    private static readonly string[] RequiredTables =
        { "head", "hhea", "hmtx", "maxp", "loca", "glyf", "cvt ", "fpgm", "prep" };

    private readonly byte[] _data;
    private readonly Dictionary<string, (uint Offset, uint Length)> _tables = new(StringComparer.Ordinal);
    private readonly Dictionary<int, ushort> _cmap = new();
    private readonly ushort[] _advanceWidths;
    private readonly uint[] _glyphOffsets;

    /// <summary>Gets the font's em square size, the unit all metrics are expressed in.</summary>
    public ushort UnitsPerEm { get; }

    /// <summary>Gets the number of glyphs in the font.</summary>
    public ushort GlyphCount { get; }

    /// <summary>Gets the PostScript name of the font, used as the PDF BaseFont.</summary>
    public string PostScriptName { get; }

    /// <summary>Gets the font bounding box in em units, as [xMin yMin xMax yMax].</summary>
    public short[] FontBBox { get; }

    /// <summary>Gets the typographic ascender in em units.</summary>
    public short Ascender { get; }

    /// <summary>Gets the typographic descender in em units (negative).</summary>
    public short Descender { get; }

    /// <summary>Gets the height of capital letters in em units.</summary>
    public short CapHeight { get; }

    /// <summary>Gets the italic angle in degrees (negative slopes to the right).</summary>
    public double ItalicAngle { get; }

    /// <summary>Gets a value indicating whether every glyph has the same advance width.</summary>
    public bool IsFixedPitch { get; }

    /// <summary>Gets a value indicating whether the font is marked as bold.</summary>
    public bool IsBold { get; }

    /// <summary>
    /// Parses a TrueType font from its raw file bytes.
    /// </summary>
    /// <param name="data">The complete contents of a .ttf file.</param>
    /// <exception cref="InvalidOperationException">The data is not a supported TrueType font.</exception>
    public TrueTypeFont(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;

        var version = ReadUInt32(0);
        if (version != 0x00010000 && version != 0x74727565) // 1.0 or 'true'
            throw new InvalidOperationException("Only TrueType outline fonts (glyf/loca) are supported.");

        var tableCount = ReadUInt16(4);
        for (var i = 0; i < tableCount; i++)
        {
            var record = (uint)(12 + 16 * i);
            var tag = Encoding.ASCII.GetString(_data, (int)record, 4);
            _tables[tag] = (ReadUInt32(record + 8), ReadUInt32(record + 12));
        }

        foreach (var required in new[] { "head", "hhea", "maxp", "hmtx", "loca", "glyf", "cmap" })
        {
            if (!_tables.ContainsKey(required))
                throw new InvalidOperationException($"Font is missing the required '{required}' table.");
        }

        var head = _tables["head"].Offset;
        UnitsPerEm = ReadUInt16(head + 18);
        FontBBox = new[]
        {
            ReadInt16(head + 36), ReadInt16(head + 38),
            ReadInt16(head + 40), ReadInt16(head + 42)
        };
        IsBold = (ReadUInt16(head + 44) & 0x01) != 0;
        var longLocaFormat = ReadInt16(head + 50) == 1;

        GlyphCount = ReadUInt16(_tables["maxp"].Offset + 4);

        var hhea = _tables["hhea"].Offset;
        Ascender = ReadInt16(hhea + 4);
        Descender = ReadInt16(hhea + 6);
        var metricCount = ReadUInt16(hhea + 34);

        PostScriptName = ReadPostScriptName();
        CapHeight = ReadCapHeight();
        (ItalicAngle, IsFixedPitch) = ReadPostTable();

        _advanceWidths = ReadHorizontalMetrics(metricCount);
        _glyphOffsets = ReadGlyphOffsets(longLocaFormat);
        ReadCharacterMap();
    }

    /// <summary>
    /// Maps a Unicode code point to a glyph index, or 0 when the font has no glyph for it.
    /// </summary>
    public ushort GetGlyphIndex(int codePoint) => _cmap.GetValueOrDefault(codePoint, (ushort)0);

    /// <summary>
    /// Determines whether the font can render the given code point.
    /// </summary>
    public bool HasGlyph(int codePoint) => GetGlyphIndex(codePoint) != 0;

    /// <summary>
    /// Gets the advance width of a glyph, expressed in 1/1000 em as PDF requires.
    /// </summary>
    public int GetAdvanceWidth(ushort glyphIndex)
    {
        if (_advanceWidths.Length == 0) return 0;
        var raw = glyphIndex < _advanceWidths.Length
            ? _advanceWidths[glyphIndex]
            : _advanceWidths[^1];
        return (int)Math.Round(raw * 1000.0 / UnitsPerEm);
    }

    /// <summary>
    /// Scales a value from font units to the PDF text space of 1/1000 em.
    /// </summary>
    public int ToPdfUnits(short value) => (int)Math.Round(value * 1000.0 / UnitsPerEm);

    /// <summary>
    /// Builds a reduced copy of the font that keeps only the requested glyphs.
    /// Glyph indices are preserved, so the caller's existing indices stay valid;
    /// unused glyphs are emptied rather than removed.
    /// </summary>
    /// <param name="glyphIndices">The glyphs the document actually draws.</param>
    /// <returns>A valid TrueType font suitable for a PDF FontFile2 stream.</returns>
    public byte[] CreateSubset(IEnumerable<ushort> glyphIndices)
    {
        ArgumentNullException.ThrowIfNull(glyphIndices);

        var keep = new HashSet<ushort> { 0 }; // .notdef is always required
        foreach (var glyph in glyphIndices)
        {
            if (glyph < GlyphCount) keep.Add(glyph);
        }
        AddCompositeComponents(keep);

        // loca and hmtx are sized by the glyph count, not by how many glyphs are
        // kept, so the font is also truncated above the highest glyph in use.
        // Indices below that stay valid, which is what lets callers keep theirs.
        var glyphCount = (ushort)(keep.Max() + 1);

        var (glyf, loca) = BuildGlyphTables(keep, glyphCount);

        var tables = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["glyf"] = glyf,
            ["loca"] = loca,
            ["head"] = BuildHead(),
            ["maxp"] = BuildMaxp(glyphCount),
            ["hhea"] = BuildHhea(glyphCount, out var metricCount),
            ["hmtx"] = BuildHmtx(glyphCount, metricCount),
        };

        foreach (var optional in new[] { "cvt ", "fpgm", "prep" })
        {
            if (_tables.ContainsKey(optional)) tables[optional] = CopyTable(optional);
        }

        return AssembleFont(tables);
    }

    /// <summary>
    /// Produces the six-letter tag PDF uses to mark an embedded font as a subset,
    /// derived from the glyphs kept so different subsets get different names.
    /// </summary>
    public static string CreateSubsetTag(IEnumerable<ushort> glyphIndices)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var glyph in glyphIndices.OrderBy(g => g))
            {
                hash = (hash ^ glyph) * 16777619u;
            }

            var tag = new char[6];
            for (var i = 0; i < 6; i++)
            {
                tag[i] = (char)('A' + hash % 26);
                hash /= 26;
            }
            return new string(tag);
        }
    }

    // --- Parsing helpers -------------------------------------------------

    private ushort[] ReadHorizontalMetrics(ushort metricCount)
    {
        if (metricCount == 0) return Array.Empty<ushort>();

        var hmtx = _tables["hmtx"].Offset;
        var widths = new ushort[GlyphCount];
        ushort last = 0;

        for (var i = 0; i < GlyphCount; i++)
        {
            if (i < metricCount) last = ReadUInt16(hmtx + (uint)(4 * i));
            widths[i] = last;
        }
        return widths;
    }

    private uint[] ReadGlyphOffsets(bool longFormat)
    {
        var loca = _tables["loca"].Offset;
        var offsets = new uint[GlyphCount + 1];

        for (var i = 0; i <= GlyphCount; i++)
        {
            offsets[i] = longFormat
                ? ReadUInt32(loca + (uint)(4 * i))
                : ReadUInt16(loca + (uint)(2 * i)) * 2u;
        }
        return offsets;
    }

    private void ReadCharacterMap()
    {
        var cmap = _tables["cmap"].Offset;
        var subtableCount = ReadUInt16(cmap + 2);

        uint format4 = 0, format12 = 0;
        for (var i = 0; i < subtableCount; i++)
        {
            var record = cmap + 4 + (uint)(8 * i);
            var platform = ReadUInt16(record);
            var encoding = ReadUInt16(record + 2);
            var subtable = cmap + ReadUInt32(record + 4);

            var isUnicode = platform == 0 || (platform == 3 && (encoding == 1 || encoding == 10));
            if (!isUnicode) continue;

            switch (ReadUInt16(subtable))
            {
                case 4: format4 = subtable; break;
                case 12: format12 = subtable; break;
            }
        }

        // Format 12 covers the full Unicode range, so prefer it when present.
        if (format12 != 0) ReadCmapFormat12(format12);
        else if (format4 != 0) ReadCmapFormat4(format4);
        else throw new InvalidOperationException("Font has no usable Unicode character map.");
    }

    private void ReadCmapFormat4(uint subtable)
    {
        var segmentCount = ReadUInt16(subtable + 6) / 2;
        var endCodes = subtable + 14;
        var startCodes = endCodes + (uint)(segmentCount * 2) + 2;
        var idDeltas = startCodes + (uint)(segmentCount * 2);
        var idRangeOffsets = idDeltas + (uint)(segmentCount * 2);

        for (var segment = 0; segment < segmentCount; segment++)
        {
            var end = ReadUInt16(endCodes + (uint)(2 * segment));
            var start = ReadUInt16(startCodes + (uint)(2 * segment));
            if (start > end || start == 0xFFFF) continue;

            var delta = ReadInt16(idDeltas + (uint)(2 * segment));
            var rangeOffsetPosition = idRangeOffsets + (uint)(2 * segment);
            var rangeOffset = ReadUInt16(rangeOffsetPosition);

            for (int code = start; code <= end; code++)
            {
                ushort glyph;
                if (rangeOffset == 0)
                {
                    glyph = (ushort)((code + delta) & 0xFFFF);
                }
                else
                {
                    var address = rangeOffsetPosition + rangeOffset + (uint)(2 * (code - start));
                    if (address + 1 >= _data.Length) continue;
                    glyph = ReadUInt16(address);
                    if (glyph != 0) glyph = (ushort)((glyph + delta) & 0xFFFF);
                }

                if (glyph != 0) _cmap[code] = glyph;
            }
        }
    }

    private void ReadCmapFormat12(uint subtable)
    {
        var groupCount = ReadUInt32(subtable + 12);
        for (uint i = 0; i < groupCount; i++)
        {
            var group = subtable + 16 + 12 * i;
            var start = ReadUInt32(group);
            var end = ReadUInt32(group + 4);
            var startGlyph = ReadUInt32(group + 8);

            // Guard against fonts declaring absurd ranges.
            if (end < start || end - start > 0x10FFFF) continue;

            for (var code = start; code <= end; code++)
            {
                var glyph = startGlyph + (code - start);
                if (glyph != 0 && glyph < GlyphCount) _cmap[(int)code] = (ushort)glyph;
            }
        }
    }

    private string ReadPostScriptName()
    {
        if (!_tables.TryGetValue("name", out var table)) return "Embedded";

        var count = ReadUInt16(table.Offset + 2);
        var storage = table.Offset + ReadUInt16(table.Offset + 4);

        for (var i = 0; i < count; i++)
        {
            var record = table.Offset + 6 + (uint)(12 * i);
            if (ReadUInt16(record + 6) != 6) continue; // name ID 6 = PostScript name

            var platform = ReadUInt16(record);
            var length = ReadUInt16(record + 8);
            var offset = storage + ReadUInt16(record + 10);
            if (offset + length > _data.Length) continue;

            // Platform 3 (Windows) stores names as UTF-16BE, platform 1 (Mac) as single bytes.
            var name = platform == 3
                ? Encoding.BigEndianUnicode.GetString(_data, (int)offset, length)
                : Encoding.ASCII.GetString(_data, (int)offset, length);

            name = new string(name.Where(c => c > 32 && c < 127 && c != '/' && c != '(' && c != ')').ToArray());
            if (name.Length > 0) return name;
        }
        return "Embedded";
    }

    private short ReadCapHeight()
    {
        // sCapHeight only exists from OS/2 version 2 onwards.
        if (_tables.TryGetValue("OS/2", out var os2) && ReadUInt16(os2.Offset) >= 2 && os2.Length >= 90)
        {
            var capHeight = ReadInt16(os2.Offset + 88);
            if (capHeight != 0) return capHeight;
        }
        // Fall back to the height of 'H', then to a share of the ascender.
        var glyph = GetGlyphIndex('H');
        if (glyph != 0)
        {
            var bounds = GetGlyphBounds(glyph);
            if (bounds.HasValue) return bounds.Value.YMax;
        }
        return (short)(Ascender * 0.7);
    }

    private (double ItalicAngle, bool IsFixedPitch) ReadPostTable()
    {
        if (!_tables.TryGetValue("post", out var post) || post.Length < 20) return (0, false);

        // italicAngle is a 16.16 fixed-point value.
        var angle = ReadInt16(post.Offset + 4) + ReadUInt16(post.Offset + 6) / 65536.0;
        return (angle, ReadUInt32(post.Offset + 12) != 0);
    }

    private (short YMin, short YMax)? GetGlyphBounds(ushort glyphIndex)
    {
        if (glyphIndex + 1 >= _glyphOffsets.Length) return null;
        var start = _tables["glyf"].Offset + _glyphOffsets[glyphIndex];
        if (_glyphOffsets[glyphIndex + 1] <= _glyphOffsets[glyphIndex]) return null;
        return (ReadInt16(start + 4), ReadInt16(start + 8));
    }

    // --- Subsetting ------------------------------------------------------

    /// <summary>
    /// Expands the glyph set with the components that composite glyphs reference,
    /// following nested composites until the set stops growing.
    /// </summary>
    private void AddCompositeComponents(HashSet<ushort> keep)
    {
        var pending = new Queue<ushort>(keep);
        while (pending.Count > 0)
        {
            var glyph = pending.Dequeue();
            if (glyph + 1 >= _glyphOffsets.Length) continue;

            var start = _glyphOffsets[glyph];
            var end = _glyphOffsets[glyph + 1];
            if (end <= start || end - start < 10) continue;

            var position = _tables["glyf"].Offset + start;
            if (ReadInt16(position) >= 0) continue; // simple glyph, no components

            position += 10;
            while (true)
            {
                var flags = ReadUInt16(position);
                var component = ReadUInt16(position + 2);
                position += 4;

                position += (uint)((flags & 0x0001) != 0 ? 4 : 2); // ARG_1_AND_2_ARE_WORDS
                if ((flags & 0x0008) != 0) position += 2;          // WE_HAVE_A_SCALE
                else if ((flags & 0x0040) != 0) position += 4;     // X_AND_Y_SCALE
                else if ((flags & 0x0080) != 0) position += 8;     // TWO_BY_TWO

                if (component < GlyphCount && keep.Add(component)) pending.Enqueue(component);
                if ((flags & 0x0020) == 0) break;                  // MORE_COMPONENTS
            }
        }
    }

    /// <summary>
    /// Rebuilds glyf and loca, copying only the glyphs to keep and leaving
    /// zero-length entries for the rest so glyph indices stay stable.
    /// </summary>
    private (byte[] Glyf, byte[] Loca) BuildGlyphTables(HashSet<ushort> keep, ushort glyphCount)
    {
        var glyf = new List<byte>();
        var loca = new byte[(glyphCount + 1) * 4]; // always the long format

        var glyfBase = _tables["glyf"].Offset;
        for (ushort glyph = 0; glyph < glyphCount; glyph++)
        {
            WriteUInt32(loca, glyph * 4, (uint)glyf.Count);

            if (!keep.Contains(glyph)) continue;

            var start = _glyphOffsets[glyph];
            var end = _glyphOffsets[glyph + 1];
            if (end <= start) continue;

            var length = (int)(end - start);
            if (glyfBase + end > _data.Length) continue;

            glyf.AddRange(_data.AsSpan((int)(glyfBase + start), length).ToArray());

            // Every glyph must start on a 4-byte boundary.
            while (glyf.Count % 4 != 0) glyf.Add(0);
        }
        WriteUInt32(loca, glyphCount * 4, (uint)glyf.Count);

        if (glyf.Count == 0) glyf.AddRange(new byte[4]);
        return (glyf.ToArray(), loca);
    }

    private byte[] BuildHead()
    {
        var head = CopyTable("head");
        WriteUInt32(head, 8, 0);           // checkSumAdjustment is recomputed by consumers
        WriteInt16(head, 50, 1);           // indexToLocFormat: we always emit long loca
        return head;
    }

    private byte[] BuildMaxp(ushort glyphCount)
    {
        var maxp = CopyTable("maxp");
        WriteUInt16(maxp, 4, glyphCount);
        return maxp;
    }

    /// <summary>
    /// Copies hhea, clamping the horizontal metric count to the truncated glyph range.
    /// </summary>
    private byte[] BuildHhea(ushort glyphCount, out ushort metricCount)
    {
        var hhea = CopyTable("hhea");
        var original = ReadUInt16(_tables["hhea"].Offset + 34);

        metricCount = Math.Min(original, glyphCount);
        WriteUInt16(hhea, 34, metricCount);
        return hhea;
    }

    /// <summary>
    /// Rebuilds hmtx for the truncated glyph range. The table stores full metrics
    /// for the first <paramref name="metricCount"/> glyphs and a left side bearing
    /// alone for the rest, which share the last advance width.
    /// </summary>
    private byte[] BuildHmtx(ushort glyphCount, ushort metricCount)
    {
        var source = _tables["hmtx"];
        var originalMetricCount = ReadUInt16(_tables["hhea"].Offset + 34);

        var hmtx = new byte[metricCount * 4 + Math.Max(0, glyphCount - metricCount) * 2];
        var position = 0;

        for (var glyph = 0; glyph < metricCount; glyph++)
        {
            var offset = source.Offset + (uint)(glyph * 4);
            if (offset + 4 > _data.Length) break;

            WriteUInt16(hmtx, position, ReadUInt16(offset));          // advance width
            WriteInt16(hmtx, position + 2, ReadInt16(offset + 2));    // left side bearing
            position += 4;
        }

        for (var glyph = metricCount; glyph < glyphCount; glyph++)
        {
            var offset = source.Offset + (uint)(originalMetricCount * 4 + (glyph - originalMetricCount) * 2);
            if (offset + 2 > _data.Length) break;

            WriteInt16(hmtx, position, ReadInt16(offset));
            position += 2;
        }

        return hmtx;
    }

    private byte[] CopyTable(string tag)
    {
        var (offset, length) = _tables[tag];
        return _data.AsSpan((int)offset, (int)length).ToArray();
    }

    /// <summary>
    /// Writes the sfnt container: directory header, table records and padded table data.
    /// </summary>
    private static byte[] AssembleFont(Dictionary<string, byte[]> tables)
    {
        var ordered = tables.OrderBy(t => t.Key, StringComparer.Ordinal).ToList();
        var count = ordered.Count;

        var searchRange = 1;
        var entrySelector = 0;
        while (searchRange * 2 <= count) { searchRange *= 2; entrySelector++; }
        searchRange *= 16;

        var directorySize = 12 + 16 * count;
        var output = new List<byte>(directorySize);

        var header = new byte[directorySize];
        WriteUInt32(header, 0, 0x00010000);
        WriteUInt16(header, 4, (ushort)count);
        WriteUInt16(header, 6, (ushort)searchRange);
        WriteUInt16(header, 8, (ushort)entrySelector);
        WriteUInt16(header, 10, (ushort)(count * 16 - searchRange));

        var dataOffset = directorySize;
        var body = new List<byte>();

        for (var i = 0; i < count; i++)
        {
            var (tag, data) = (ordered[i].Key, ordered[i].Value);
            var record = 12 + 16 * i;

            Encoding.ASCII.GetBytes(tag).CopyTo(header, record);
            WriteUInt32(header, record + 4, CalculateChecksum(data));
            WriteUInt32(header, record + 8, (uint)dataOffset);
            WriteUInt32(header, record + 12, (uint)data.Length);

            body.AddRange(data);
            var padding = (4 - data.Length % 4) % 4;
            body.AddRange(new byte[padding]);
            dataOffset += data.Length + padding;
        }

        output.AddRange(header);
        output.AddRange(body);
        return output.ToArray();
    }

    private static uint CalculateChecksum(byte[] table)
    {
        uint sum = 0;
        for (var i = 0; i < table.Length; i += 4)
        {
            uint word = 0;
            for (var b = 0; b < 4; b++)
            {
                word <<= 8;
                if (i + b < table.Length) word |= table[i + b];
            }
            unchecked { sum += word; }
        }
        return sum;
    }

    // --- Big-endian primitives -------------------------------------------

    private ushort ReadUInt16(uint offset) => (ushort)((_data[offset] << 8) | _data[offset + 1]);

    private short ReadInt16(uint offset) => (short)((_data[offset] << 8) | _data[offset + 1]);

    private uint ReadUInt32(uint offset) =>
        ((uint)_data[offset] << 24) | ((uint)_data[offset + 1] << 16) |
        ((uint)_data[offset + 2] << 8) | _data[offset + 3];

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value >> 8);
        buffer[offset + 1] = (byte)value;
    }

    private static void WriteInt16(byte[] buffer, int offset, short value) =>
        WriteUInt16(buffer, offset, (ushort)value);

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
