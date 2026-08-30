using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PdfTickleSharp.Core.Document;
using PdfTickleSharp.Core.Graphics;
using PdfTickleSharp.Core.Text;

namespace PdfTickleSharp.Core.IO;

/// <summary>
/// Writes PDF documents to files or streams in PDF 1.7 format.
/// Fonts are embedded as subsetted Identity-H CID fonts so any Unicode text the
/// bundled fonts cover renders and remains searchable, and images are converted
/// into the sample formats PDF understands.
/// </summary>
public class PdfWriter
{
    /// <summary>
    /// Gets or sets whether content, font and image streams are Flate compressed.
    /// Turn this off to produce a readable PDF when debugging output.
    /// </summary>
    public bool CompressStreams { get; set; } = true;

    private readonly List<PdfObject> _objects = new();
    private readonly Dictionary<LoadedFont, FontUsage> _fonts = new();
    private readonly List<EmbeddedImage> _images = new();
    private readonly Dictionary<byte[], int> _imageLookup = new(ByteArrayComparer.Instance);

    /// <summary>
    /// Writes the PDF document to the specified file path.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="filePath">The destination file path.</param>
    public void WriteToFile(PdfDocument document, string filePath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(filePath);

        File.WriteAllBytes(filePath, GeneratePdf(document));
    }

    /// <summary>
    /// Writes the PDF document to the specified stream.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="stream">The destination stream.</param>
    public void WriteToStream(PdfDocument document, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);

        var pdf = GeneratePdf(document);
        stream.Write(pdf, 0, pdf.Length);
    }

    /// <summary>
    /// Generates the complete PDF file as a byte array.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <returns>The PDF file contents.</returns>
    public byte[] GeneratePdf(PdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // A PDF must contain at least one page; failing here is clearer than
        // writing a file that every viewer rejects.
        if (document.PageCount == 0)
            throw new InvalidOperationException(
                "Cannot generate a PDF from a document with no pages. Add a page with AddPage() first.");

        FontManager.Initialize();
        Reset();

        // Work out which fonts, glyphs and images the document needs before
        // emitting anything, so every resource exists by the time pages use it.
        AnalyzeDocument(document);

        var catalog = AllocateObject();
        var pagesRoot = AllocateObject();

        WriteFontObjects();
        WriteImageObjects();

        var pageObjects = WritePageObjects(document, pagesRoot.Number);
        var info = WriteInfoObject(document.Metadata);

        catalog.Dictionary =
            "<< /Type /Catalog " +
            $"/Pages {pagesRoot.Number} 0 R " +
            "/ViewerPreferences << /FitWindow true /CenterWindow true >> >>";

        var kids = string.Join(" ", pageObjects.Select(page => $"{page.Number} 0 R"));
        pagesRoot.Dictionary = $"<< /Type /Pages /Kids [{kids}] /Count {pageObjects.Count} >>";

        return Serialize(catalog, info, document);
    }

    private void Reset()
    {
        _objects.Clear();
        _fonts.Clear();
        _images.Clear();
        _imageLookup.Clear();
    }

    // --- Analysis --------------------------------------------------------

    /// <summary>
    /// Walks every page to record which glyphs each font must embed and to decode
    /// each distinct image exactly once.
    /// </summary>
    private void AnalyzeDocument(PdfDocument document)
    {
        foreach (var page in document.Pages)
        {
            foreach (var content in page.GetContents())
            {
                switch (content)
                {
                    case TextContent text:
                        RegisterText(text.Text, text.Format);
                        break;

                    case TextFlowContent flow:
                        RegisterText(flow.Text, flow.Format);
                        break;

                    case ImageContent image:
                        RegisterImage(image.ImageData);
                        break;
                }
            }
        }
    }

    private void RegisterText(string text, TextFormat format)
    {
        foreach (var run in FontManager.SplitIntoRuns(text, format))
        {
            if (!_fonts.TryGetValue(run.Font, out var usage))
            {
                usage = new FontUsage(run.Font, _fonts.Count);
                _fonts[run.Font] = usage;
            }
            usage.Record(run.Text);
        }
    }

    private void RegisterImage(byte[] imageData)
    {
        if (imageData.Length == 0 || _imageLookup.ContainsKey(imageData)) return;

        _imageLookup[imageData] = _images.Count;
        _images.Add(new EmbeddedImage(_images.Count, ImageDecoder.Decode(imageData)));
    }

    // --- Font objects ----------------------------------------------------

    /// <summary>
    /// Emits, for every font in use, the five objects a subsetted CID font needs:
    /// the font program, its descriptor, the CID font, a ToUnicode map and the
    /// Type0 font that page resources refer to.
    /// </summary>
    private void WriteFontObjects()
    {
        foreach (var usage in _fonts.Values.OrderBy(f => f.ResourceIndex))
        {
            var font = usage.Font.Font;
            var glyphs = usage.Glyphs.OrderBy(g => g).ToList();

            var subset = font.CreateSubset(glyphs);
            var subsetName = $"{TrueTypeFont.CreateSubsetTag(glyphs)}+{font.PostScriptName}";

            var fontFile = AddStreamObject($"/Length1 {subset.Length}", subset);

            var descriptor = AllocateObject();
            descriptor.Dictionary =
                "<< /Type /FontDescriptor " +
                $"/FontName /{subsetName} " +
                $"/Flags {GetFontFlags(font)} " +
                $"/FontBBox [{string.Join(" ", font.FontBBox.Select(font.ToPdfUnits))}] " +
                $"/ItalicAngle {Number(font.ItalicAngle)} " +
                $"/Ascent {font.ToPdfUnits(font.Ascender)} " +
                $"/Descent {font.ToPdfUnits(font.Descender)} " +
                $"/CapHeight {font.ToPdfUnits(font.CapHeight)} " +
                $"/StemV {(font.IsBold ? 160 : 80)} " +
                $"/FontFile2 {fontFile.Number} 0 R >>";

            var cidFont = AllocateObject();
            cidFont.Dictionary =
                "<< /Type /Font /Subtype /CIDFontType2 " +
                $"/BaseFont /{subsetName} " +
                "/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> " +
                $"/FontDescriptor {descriptor.Number} 0 R " +
                "/DW 1000 " +
                $"/W {BuildWidthArray(font, glyphs)} " +
                "/CIDToGIDMap /Identity >>";

            var toUnicode = AddStreamObject(string.Empty, BuildToUnicodeCMap(usage));

            var type0 = AllocateObject();
            type0.Dictionary =
                "<< /Type /Font /Subtype /Type0 " +
                $"/BaseFont /{subsetName} " +
                "/Encoding /Identity-H " +
                $"/DescendantFonts [{cidFont.Number} 0 R] " +
                $"/ToUnicode {toUnicode.Number} 0 R >>";

            usage.ObjectNumber = type0.Number;
        }
    }

    private static int GetFontFlags(TrueTypeFont font)
    {
        var flags = 4; // Symbolic: the font supplies its own encoding via Identity-H
        if (font.IsFixedPitch) flags |= 1;
        if (font.ItalicAngle != 0) flags |= 64;
        return flags;
    }

    /// <summary>
    /// Builds the CID font W array, grouping runs of consecutive glyph indices
    /// so the array stays compact.
    /// </summary>
    private static string BuildWidthArray(TrueTypeFont font, List<ushort> glyphs)
    {
        var array = new StringBuilder("[");

        for (var i = 0; i < glyphs.Count;)
        {
            var start = i;
            while (i + 1 < glyphs.Count && glyphs[i + 1] == glyphs[i] + 1) i++;

            var widths = Enumerable.Range(start, i - start + 1)
                .Select(index => font.GetAdvanceWidth(glyphs[index]).ToString(CultureInfo.InvariantCulture));

            array.Append($" {glyphs[start]} [{string.Join(" ", widths)}]");
            i++;
        }

        array.Append(" ]");
        return array.ToString();
    }

    /// <summary>
    /// Builds a ToUnicode CMap so that text drawn as glyph indices can still be
    /// selected, copied and searched in a PDF viewer.
    /// </summary>
    private static byte[] BuildToUnicodeCMap(FontUsage usage)
    {
        var entries = usage.GlyphToText.OrderBy(pair => pair.Key).ToList();

        var cmap = new StringBuilder();
        cmap.AppendLine("/CIDInit /ProcSet findresource begin");
        cmap.AppendLine("12 dict begin");
        cmap.AppendLine("begincmap");
        cmap.AppendLine("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def");
        cmap.AppendLine("/CMapName /Adobe-Identity-UCS def");
        cmap.AppendLine("/CMapType 2 def");
        cmap.AppendLine("1 begincodespacerange");
        cmap.AppendLine("<0000> <FFFF>");
        cmap.AppendLine("endcodespacerange");

        // A bfchar section may hold at most 100 mappings.
        foreach (var chunk in entries.Chunk(100))
        {
            cmap.AppendLine($"{chunk.Length} beginbfchar");
            foreach (var (glyph, text) in chunk)
            {
                var utf16 = string.Concat(
                    Encoding.BigEndianUnicode.GetBytes(text).Select(b => b.ToString("X2")));
                cmap.AppendLine($"<{glyph:X4}> <{utf16}>");
            }
            cmap.AppendLine("endbfchar");
        }

        cmap.AppendLine("endcmap");
        cmap.AppendLine("CMapName currentdict /CMap defineresource pop");
        cmap.AppendLine("end");
        cmap.AppendLine("end");

        return Encoding.ASCII.GetBytes(cmap.ToString());
    }

    // --- Image objects ---------------------------------------------------

    /// <summary>
    /// Emits an image XObject per decoded image, plus a soft mask object when the
    /// image carries an alpha channel.
    /// </summary>
    private void WriteImageObjects()
    {
        foreach (var image in _images)
        {
            var decoded = image.Image;
            var softMaskReference = string.Empty;

            if (decoded.SoftMask != null)
            {
                var mask = AddStreamObject(
                    "/Type /XObject /Subtype /Image " +
                    $"/Width {decoded.Width} /Height {decoded.Height} " +
                    "/ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /FlateDecode",
                    decoded.SoftMask,
                    alreadyCompressed: true);

                softMaskReference = $" /SMask {mask.Number} 0 R";
            }

            var xObject = AddStreamObject(
                "/Type /XObject /Subtype /Image " +
                $"/Width {decoded.Width} /Height {decoded.Height} " +
                $"/ColorSpace {decoded.ColorSpace} " +
                $"/BitsPerComponent {decoded.BitsPerComponent} " +
                $"/Filter {decoded.Filter}" +
                softMaskReference,
                decoded.Data,
                alreadyCompressed: true);

            image.ObjectNumber = xObject.Number;
        }
    }

    // --- Page objects ----------------------------------------------------

    private List<PdfObject> WritePageObjects(PdfDocument document, int parentNumber)
    {
        var pages = new List<PdfObject>();

        foreach (var page in document.Pages)
        {
            var contentStream = AddStreamObject(string.Empty, BuildContentStream(page));

            var pageObject = AllocateObject();
            pageObject.Dictionary =
                "<< /Type /Page " +
                $"/Parent {parentNumber} 0 R " +
                $"/MediaBox [0 0 {Number(page.PageSize.Width)} {Number(page.PageSize.Height)}] " +
                $"/Contents {contentStream.Number} 0 R " +
                $"/Resources {BuildResourceDictionary(page)} >>";

            pages.Add(pageObject);
        }

        return pages;
    }

    /// <summary>
    /// Builds a page's resource dictionary, listing only the fonts and images that
    /// page actually draws.
    /// </summary>
    private string BuildResourceDictionary(PdfPage page)
    {
        var contents = page.GetContents().ToList();

        var usedFonts = new SortedDictionary<int, FontUsage>();
        foreach (var content in contents)
        {
            var (text, format) = content switch
            {
                TextContent t => (t.Text, t.Format),
                TextFlowContent f => (f.Text, f.Format),
                _ => (null, null)
            };
            if (text == null || format == null) continue;

            foreach (var run in FontManager.SplitIntoRuns(text, format))
            {
                var usage = _fonts[run.Font];
                usedFonts[usage.ResourceIndex] = usage;
            }
        }

        var usedImages = contents.OfType<ImageContent>()
            .Where(image => _imageLookup.ContainsKey(image.ImageData))
            .Select(image => _images[_imageLookup[image.ImageData]])
            .DistinctBy(image => image.ResourceIndex)
            .OrderBy(image => image.ResourceIndex)
            .ToList();

        var resources = new StringBuilder("<< /ProcSet [/PDF /Text /ImageB /ImageC]");

        if (usedFonts.Count > 0)
        {
            var fonts = usedFonts.Values.Select(f => $"/F{f.ResourceIndex} {f.ObjectNumber} 0 R");
            resources.Append($" /Font << {string.Join(" ", fonts)} >>");
        }

        if (usedImages.Count > 0)
        {
            var images = usedImages.Select(i => $"/Im{i.ResourceIndex} {i.ObjectNumber} 0 R");
            resources.Append($" /XObject << {string.Join(" ", images)} >>");
        }

        resources.Append(" >>");
        return resources.ToString();
    }

    /// <summary>
    /// Renders a page's contents to a PDF content stream. Elements are drawn in the
    /// order they were added, so later elements paint over earlier ones.
    /// </summary>
    private byte[] BuildContentStream(PdfPage page)
    {
        var content = new StringBuilder();

        foreach (var element in page.GetContents())
        {
            switch (element)
            {
                case TextContent text:
                    AppendTextLine(content, text.Text, text.X, text.Y, text.Format);
                    break;

                case TextFlowContent flow:
                    AppendTextFlow(content, flow);
                    break;

                case LineContent line:
                    AppendLine(content, line);
                    break;

                case RectangleContent rectangle:
                    AppendRectangle(content, rectangle);
                    break;

                case CircleContent circle:
                    AppendCircle(content, circle);
                    break;

                case ImageContent image:
                    AppendImage(content, image);
                    break;
            }
        }

        return Encoding.ASCII.GetBytes(content.ToString());
    }

    /// <summary>
    /// Draws a single line of text. The line may be split across several fonts;
    /// consecutive show operators continue from the current text position, so no
    /// repositioning is needed between runs.
    /// </summary>
    private void AppendTextLine(StringBuilder content, string text, double x, double y, TextFormat format)
    {
        var runs = FontManager.SplitIntoRuns(text, format);
        if (runs.Count == 0) return;

        var width = runs.Sum(run => run.Measure(format.FontSize));
        var startX = format.Alignment switch
        {
            TextAlignment.Center => x - width / 2,
            TextAlignment.Right => x - width,
            _ => x
        };

        content.AppendLine("BT");
        content.AppendLine($"{FormatColor(format.Color)} rg");
        content.AppendLine($"{Number(startX)} {Number(y)} Td");

        foreach (var run in runs)
        {
            var usage = _fonts[run.Font];
            var glyphs = string.Concat(run.Font.GetGlyphs(run.Text).Select(g => g.ToString("X4")));

            content.AppendLine($"/F{usage.ResourceIndex} {Number(format.FontSize)} Tf");
            content.AppendLine($"<{glyphs}> Tj");
        }

        content.AppendLine("ET");
    }

    private void AppendTextFlow(StringBuilder content, TextFlowContent flow)
    {
        var lineHeight = GetLineHeight(flow.Format);
        var y = flow.Y;

        foreach (var line in WrapText(flow.Text, flow.Width, flow.Format))
        {
            // Alignment inside a flow is measured against the wrap width.
            var x = flow.Format.Alignment switch
            {
                TextAlignment.Center => flow.X + flow.Width / 2,
                TextAlignment.Right => flow.X + flow.Width,
                _ => flow.X
            };

            AppendTextLine(content, line, x, y, flow.Format);
            y -= lineHeight;
        }
    }

    private static void AppendLine(StringBuilder content, LineContent line)
    {
        content.AppendLine("q");
        content.AppendLine($"{FormatColor(line.Color)} RG");
        content.AppendLine($"{Number(line.Width)} w");
        content.AppendLine("1 J"); // round caps read better on thick strokes
        content.AppendLine($"{Number(line.X1)} {Number(line.Y1)} m");
        content.AppendLine($"{Number(line.X2)} {Number(line.Y2)} l");
        content.AppendLine("S");
        content.AppendLine("Q");
    }

    private static void AppendRectangle(StringBuilder content, RectangleContent rectangle)
    {
        content.AppendLine("q");
        content.AppendLine(rectangle.Filled
            ? $"{FormatColor(rectangle.Color)} rg"
            : $"{FormatColor(rectangle.Color)} RG");
        content.AppendLine($"{Number(rectangle.LineWidth)} w");
        content.AppendLine(
            $"{Number(rectangle.X)} {Number(rectangle.Y)} " +
            $"{Number(rectangle.Width)} {Number(rectangle.Height)} re");
        content.AppendLine(rectangle.Filled ? "f" : "S");
        content.AppendLine("Q");
    }

    private static void AppendCircle(StringBuilder content, CircleContent circle)
    {
        content.AppendLine("q");
        content.AppendLine(circle.Filled
            ? $"{FormatColor(circle.Color)} rg"
            : $"{FormatColor(circle.Color)} RG");
        content.AppendLine($"{Number(circle.LineWidth)} w");
        AppendCirclePath(content, circle.CenterX, circle.CenterY, circle.Radius);
        content.AppendLine(circle.Filled ? "f" : "S");
        content.AppendLine("Q");
    }

    /// <summary>
    /// Approximates a circle with four cubic Bezier arcs.
    /// </summary>
    private static void AppendCirclePath(StringBuilder content, double cx, double cy, double r)
    {
        // Control-point distance that makes a cubic Bezier match a quarter circle.
        const double k = 0.5522847498;
        var offset = k * r;

        content.AppendLine($"{Number(cx)} {Number(cy + r)} m");
        content.AppendLine($"{Number(cx + offset)} {Number(cy + r)} {Number(cx + r)} {Number(cy + offset)} {Number(cx + r)} {Number(cy)} c");
        content.AppendLine($"{Number(cx + r)} {Number(cy - offset)} {Number(cx + offset)} {Number(cy - r)} {Number(cx)} {Number(cy - r)} c");
        content.AppendLine($"{Number(cx - offset)} {Number(cy - r)} {Number(cx - r)} {Number(cy - offset)} {Number(cx - r)} {Number(cy)} c");
        content.AppendLine($"{Number(cx - r)} {Number(cy + offset)} {Number(cx - offset)} {Number(cy + r)} {Number(cx)} {Number(cy + r)} c");
        content.AppendLine("h");
    }

    /// <summary>
    /// Places an image. The transformation matrix scales the unit square the image
    /// occupies to the requested size and moves it to the requested position.
    /// </summary>
    private void AppendImage(StringBuilder content, ImageContent image)
    {
        if (!_imageLookup.TryGetValue(image.ImageData, out var index)) return;

        content.AppendLine("q");
        content.AppendLine(
            $"{Number(image.Width)} 0 0 {Number(image.Height)} " +
            $"{Number(image.X)} {Number(image.Y)} cm");
        content.AppendLine($"/Im{_images[index].ResourceIndex} Do");
        content.AppendLine("Q");
    }

    // --- Text layout -----------------------------------------------------

    /// <summary>
    /// Gets the distance between baselines, derived from the font's own ascent and
    /// descent so that the spacing suits the typeface rather than assuming the em size.
    /// </summary>
    private static double GetLineHeight(TextFormat format)
    {
        var font = FontManager.Resolve(format).Font;
        var naturalHeight = (font.Ascender - font.Descender) / (double)font.UnitsPerEm;
        return format.FontSize * naturalHeight * format.LineSpacing;
    }

    /// <summary>
    /// Breaks text into lines that fit the given width, honouring explicit line
    /// breaks and splitting any single word too long to fit.
    /// </summary>
    private static List<string> WrapText(string text, double maxWidth, TextFormat format)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;

        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var current = new StringBuilder();

            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";

                if (FontManager.MeasureText(candidate, format) <= maxWidth)
                {
                    current.Clear();
                    current.Append(candidate);
                    continue;
                }

                if (current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }

                // A word that cannot fit on a line of its own is split by character.
                if (FontManager.MeasureText(word, format) > maxWidth)
                {
                    foreach (var piece in BreakLongWord(word, maxWidth, format))
                    {
                        if (current.Length > 0) lines.Add(current.ToString());
                        current.Clear();
                        current.Append(piece);
                    }
                }
                else
                {
                    current.Append(word);
                }
            }

            if (current.Length > 0) lines.Add(current.ToString());
        }

        return lines;
    }

    private static IEnumerable<string> BreakLongWord(string word, double maxWidth, TextFormat format)
    {
        var piece = new StringBuilder();

        foreach (var character in word)
        {
            var candidate = piece.ToString() + character;

            if (piece.Length > 0 && FontManager.MeasureText(candidate, format) > maxWidth)
            {
                yield return piece.ToString();
                piece.Clear();
            }
            piece.Append(character);
        }

        if (piece.Length > 0) yield return piece.ToString();
    }

    // --- Document information --------------------------------------------

    private PdfObject WriteInfoObject(PdfMetadata metadata)
    {
        var info = new StringBuilder("<<");

        AppendTextEntry(info, "Title", metadata.Title);
        AppendTextEntry(info, "Author", metadata.Author);
        AppendTextEntry(info, "Subject", metadata.Subject);
        AppendTextEntry(info, "Keywords", metadata.Keywords);
        AppendTextEntry(info, "Creator", metadata.Creator);
        AppendTextEntry(info, "Producer", metadata.Producer);

        info.Append($" /CreationDate {FormatDate(metadata.CreationDate)}");
        info.Append($" /ModDate {FormatDate(metadata.ModificationDate)}");
        info.Append(" >>");

        var obj = AllocateObject();
        obj.Dictionary = info.ToString();
        return obj;
    }

    private static void AppendTextEntry(StringBuilder builder, string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        builder.Append($" /{key} {FormatTextString(value)}");
    }

    /// <summary>
    /// Formats a PDF text string, escaping the literal form for ASCII and falling
    /// back to a UTF-16 hex string when the value needs characters ASCII lacks.
    /// </summary>
    private static string FormatTextString(string value)
    {
        if (value.All(c => c >= 32 && c < 127))
        {
            var escaped = value
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
            return $"({escaped})";
        }

        // The leading byte order mark tells readers the string is UTF-16BE.
        var bytes = Encoding.BigEndianUnicode.GetBytes("\uFEFF" + value);
        return $"<{string.Concat(bytes.Select(b => b.ToString("X2")))}>";
    }

    private static string FormatDate(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        var sign = offset < TimeSpan.Zero ? '-' : '+';

        return $"(D:{local:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):D2}'{Math.Abs(offset.Minutes):D2}')";
    }

    // --- Serialization ---------------------------------------------------

    /// <summary>
    /// Writes the header, every object, the cross-reference table and the trailer.
    /// </summary>
    private byte[] Serialize(PdfObject catalog, PdfObject info, PdfDocument document)
    {
        using var buffer = new MemoryStream();

        Write(buffer, "%PDF-1.7\n");
        // A comment with high bytes marks the file as binary for transfer tools.
        buffer.Write([0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A]);

        var offsets = new long[_objects.Count];

        foreach (var obj in _objects)
        {
            offsets[obj.Number - 1] = buffer.Position;

            Write(buffer, $"{obj.Number} 0 obj\n");
            Write(buffer, obj.Dictionary + "\n");

            if (obj.Stream != null)
            {
                Write(buffer, "stream\n");
                buffer.Write(obj.Stream, 0, obj.Stream.Length);
                Write(buffer, "\nendstream\n");
            }

            Write(buffer, "endobj\n");
        }

        var xrefOffset = buffer.Position;
        Write(buffer, $"xref\n0 {_objects.Count + 1}\n");
        Write(buffer, "0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            Write(buffer, $"{offset:D10} 00000 n \n");
        }

        var id = CreateDocumentId(document);
        Write(buffer, "trailer\n<< " +
                      $"/Size {_objects.Count + 1} " +
                      $"/Root {catalog.Number} 0 R " +
                      $"/Info {info.Number} 0 R " +
                      $"/ID [<{id}> <{id}>] >>\n");
        Write(buffer, $"startxref\n{xrefOffset}\n%%EOF\n");

        return buffer.ToArray();
    }

    /// <summary>
    /// Derives a stable identifier for the document from its metadata and shape.
    /// </summary>
    private string CreateDocumentId(PdfDocument document)
    {
        var seed = $"{document.Metadata.Title}|{document.Metadata.Author}|" +
                   $"{document.PageCount}|{_objects.Count}|{document.Metadata.CreationDate.Ticks}";

        var hash = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return string.Concat(hash.Select(b => b.ToString("X2")));
    }

    private static void Write(Stream stream, string ascii)
    {
        var bytes = Encoding.ASCII.GetBytes(ascii);
        stream.Write(bytes, 0, bytes.Length);
    }

    // --- Object helpers --------------------------------------------------

    private PdfObject AllocateObject()
    {
        var obj = new PdfObject(_objects.Count + 1);
        _objects.Add(obj);
        return obj;
    }

    /// <summary>
    /// Adds an object whose body is a stream, compressing it unless the data is
    /// already in its final encoded form.
    /// </summary>
    /// <param name="entries">Extra dictionary entries, without /Length or the braces.</param>
    /// <param name="data">The stream payload.</param>
    /// <param name="alreadyCompressed">True when the payload carries its own filter.</param>
    private PdfObject AddStreamObject(string entries, byte[] data, bool alreadyCompressed = false)
    {
        var filter = string.Empty;

        if (!alreadyCompressed && CompressStreams)
        {
            data = ImageDecoder.Deflate(data);
            filter = " /Filter /FlateDecode";
        }

        var obj = AllocateObject();
        obj.Stream = data;
        obj.Dictionary = $"<<{(entries.Length > 0 ? " " + entries : string.Empty)}{filter} /Length {data.Length} >>";
        return obj;
    }

    private static string FormatColor(Color color) =>
        $"{Number(color.NormalizedR)} {Number(color.NormalizedG)} {Number(color.NormalizedB)}";

    /// <summary>
    /// Formats a number for PDF output: invariant, never in exponent notation and
    /// without trailing zeros.
    /// </summary>
    private static string Number(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
        return Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);
    }

    // --- Internal state --------------------------------------------------

    private sealed class PdfObject
    {
        public int Number { get; }
        public string Dictionary { get; set; } = "<< >>";
        public byte[]? Stream { get; set; }

        public PdfObject(int number) => Number = number;
    }

    /// <summary>
    /// Tracks which glyphs of a font a document uses, and what text each glyph
    /// came from so a ToUnicode map can be produced.
    /// </summary>
    private sealed class FontUsage
    {
        public LoadedFont Font { get; }
        public int ResourceIndex { get; }
        public int ObjectNumber { get; set; }
        public HashSet<ushort> Glyphs { get; } = new();
        public Dictionary<ushort, string> GlyphToText { get; } = new();

        public FontUsage(LoadedFont font, int resourceIndex)
        {
            Font = font;
            ResourceIndex = resourceIndex;
        }

        /// <summary>
        /// Records the glyphs needed to draw the given text, along with the
        /// characters they came from so a ToUnicode map can be built.
        /// </summary>
        public void Record(string text)
        {
            foreach (var (glyph, source) in Font.GetGlyphMappings(text))
            {
                Glyphs.Add(glyph);
                GlyphToText.TryAdd(glyph, source);
            }
        }
    }

    private sealed class EmbeddedImage
    {
        public int ResourceIndex { get; }
        public DecodedImage Image { get; }
        public int ObjectNumber { get; set; }

        public EmbeddedImage(int resourceIndex, DecodedImage image)
        {
            ResourceIndex = resourceIndex;
            Image = image;
        }
    }

    /// <summary>
    /// Compares byte arrays by content, so the same image supplied as two separate
    /// arrays is embedded only once.
    /// </summary>
    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new();

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            // Hash the length plus a sample of the contents; enough to separate
            // distinct images without walking megabytes of pixel data.
            var hash = new HashCode();
            hash.Add(obj.Length);

            var step = Math.Max(1, obj.Length / 64);
            for (var i = 0; i < obj.Length; i += step) hash.Add(obj[i]);

            return hash.ToHashCode();
        }
    }
}
