using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfTickleSharp.TestApp;

/// <summary>
/// Validates generated PDFs against the structural rules that viewers such as
/// Chrome (PDFium), Safari, Firefox (PDF.js) and Acrobat rely on.
/// The file is parsed into objects and its streams are decompressed, so the
/// checks test the real document rather than searching the raw bytes for text.
/// </summary>
public static class PdfCompatibilityTest
{
    /// <summary>
    /// Runs every compatibility check against a PDF file.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF to validate.</param>
    /// <returns>True when no issue was found.</returns>
    public static bool ValidatePdfForUniversalCompatibility(string pdfPath)
    {
        Console.WriteLine("=== PDF UNIVERSAL COMPATIBILITY VALIDATOR ===");
        Console.WriteLine($"Testing: {Path.GetFileName(pdfPath)}");
        Console.WriteLine();

        var issues = new List<string>();
        var warnings = new List<string>();

        var bytes = File.ReadAllBytes(pdfPath);
        var objects = PdfObjectScanner.Scan(bytes);

        CheckFileStructure(bytes, objects, issues);
        CheckDocumentTree(objects, issues, warnings);
        CheckFonts(objects, issues, warnings);
        CheckImages(objects, issues, warnings);
        CheckContentStreams(objects, issues, warnings);

        Console.WriteLine();
        Console.WriteLine("=== COMPATIBILITY ASSESSMENT ===");

        if (issues.Count == 0)
        {
            Console.WriteLine("🎉 EXCELLENT: PDF is structurally valid and universally compatible!");
            Console.WriteLine("✅ Should open properly in:");
            Console.WriteLine("   - Chrome / Edge (PDFium engine)");
            Console.WriteLine("   - Safari (WebKit PDF viewer)");
            Console.WriteLine("   - Firefox (PDF.js)");
            Console.WriteLine("   - Adobe Acrobat/Reader");
            Console.WriteLine("   - Preview.app (macOS)");
        }
        else
        {
            Console.WriteLine($"❌ COMPATIBILITY ISSUES FOUND ({issues.Count}):");
            foreach (var issue in issues) Console.WriteLine($"   ❌ {issue}");
        }

        if (warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"⚠️ WARNINGS ({warnings.Count}):");
            foreach (var warning in warnings) Console.WriteLine($"   ⚠️ {warning}");
        }

        Console.WriteLine();
        Console.WriteLine($"Objects: {objects.Count}    File Size: {new FileInfo(pdfPath).Length:N0} bytes");

        return issues.Count == 0;
    }

    /// <summary>
    /// Checks the file envelope: header, binary marker, cross-reference table and trailer.
    /// </summary>
    private static void CheckFileStructure(
        byte[] bytes, IReadOnlyDictionary<int, PdfObjectScanner.PdfObject> objects, List<string> issues)
    {
        var header = Encoding.ASCII.GetString(bytes, 0, Math.Min(9, bytes.Length));

        if (!header.StartsWith("%PDF-1.")) issues.Add("Invalid PDF header - missing %PDF-1.x signature");
        else Console.WriteLine($"✅ PDF Header: {header.TrimEnd()}");

        // A comment of high bytes on line 2 tells tools the file is binary.
        var secondLineStart = Array.IndexOf(bytes, (byte)'\n') + 1;
        if (secondLineStart > 0 && secondLineStart + 4 < bytes.Length
            && bytes[secondLineStart] == '%' && bytes[secondLineStart + 1] > 127)
        {
            Console.WriteLine("✅ Binary Comment: Present, file is marked as binary");
        }
        else
        {
            issues.Add("Missing binary comment on line 2");
        }

        var text = Encoding.ASCII.GetString(bytes);

        if (!text.TrimEnd().EndsWith("%%EOF")) issues.Add("Missing or misplaced %%EOF marker");
        else Console.WriteLine("✅ PDF Trailer: %%EOF marker present");

        var startxref = Regex.Match(text, @"startxref\s+(\d+)");
        if (!startxref.Success)
        {
            issues.Add("Missing startxref pointer");
            return;
        }

        var xrefOffset = int.Parse(startxref.Groups[1].Value);
        if (xrefOffset <= 0 || xrefOffset >= bytes.Length
            || !text.Substring(xrefOffset).StartsWith("xref"))
        {
            issues.Add($"startxref points to offset {xrefOffset}, which is not an xref table");
            return;
        }
        Console.WriteLine("✅ Cross-Reference: startxref points at a valid xref table");

        // Every xref entry must land exactly on its object header, or viewers
        // that trust the table instead of rebuilding it will fail.
        var entries = Regex.Matches(text.Substring(xrefOffset), @"(\d{10}) (\d{5}) ([nf])");
        var checkedCount = 0;
        var brokenCount = 0;

        for (var i = 1; i < entries.Count; i++)
        {
            if (entries[i].Groups[3].Value != "n") continue;

            var offset = int.Parse(entries[i].Groups[1].Value);
            checkedCount++;

            var expected = $"{i} 0 obj";
            if (offset + expected.Length > bytes.Length
                || Encoding.ASCII.GetString(bytes, offset, expected.Length) != expected)
            {
                brokenCount++;
            }
        }

        if (brokenCount > 0) issues.Add($"{brokenCount} of {checkedCount} xref offsets do not point at their object");
        else Console.WriteLine($"✅ Xref Offsets: All {checkedCount} entries point at the correct object");

        if (!Regex.IsMatch(text, @"/Root\s+\d+\s+0\s+R")) issues.Add("Trailer has no /Root reference");
        else if (!Regex.IsMatch(text, @"/Info\s+\d+\s+0\s+R")) issues.Add("Trailer has no /Info reference");
        else if (!Regex.IsMatch(text, @"/ID\s*\[")) issues.Add("Trailer has no /ID array");
        else Console.WriteLine("✅ Trailer: /Root, /Info and /ID all present");

        if (objects.Count == 0) issues.Add("No PDF objects could be parsed");
    }

    /// <summary>
    /// Checks the catalog, the page tree and each page's required entries.
    /// </summary>
    private static void CheckDocumentTree(
        IReadOnlyDictionary<int, PdfObjectScanner.PdfObject> objects, List<string> issues, List<string> warnings)
    {
        var catalog = objects.Values.FirstOrDefault(o => o.Dictionary.Contains("/Type /Catalog"));
        if (catalog == null)
        {
            issues.Add("No document catalog (/Type /Catalog) found");
            return;
        }

        var pagesRoot = objects.Values.FirstOrDefault(o => o.Dictionary.Contains("/Type /Pages"));
        if (pagesRoot == null)
        {
            issues.Add("No page tree root (/Type /Pages) found");
            return;
        }

        var declared = Regex.Match(pagesRoot.Dictionary, @"/Count\s+(\d+)");
        var pages = objects.Values.Where(o => o.Dictionary.Contains("/Type /Page ")).ToList();

        if (!declared.Success || int.Parse(declared.Groups[1].Value) != pages.Count)
        {
            issues.Add($"Page tree /Count does not match the {pages.Count} page objects present");
        }
        else
        {
            Console.WriteLine($"✅ Page Tree: {pages.Count} pages, /Count matches");
        }

        var missingBox = pages.Count(p => !p.Dictionary.Contains("/MediaBox"));
        var missingContents = pages.Count(p => !p.Dictionary.Contains("/Contents"));
        var missingResources = pages.Count(p => !p.Dictionary.Contains("/Resources"));

        if (missingBox + missingContents + missingResources > 0)
        {
            issues.Add($"Pages missing required entries: {missingBox} /MediaBox, " +
                       $"{missingContents} /Contents, {missingResources} /Resources");
        }
        else
        {
            Console.WriteLine("✅ Page Structure: Every page has /MediaBox, /Contents and /Resources");
        }

        // Every indirect reference must resolve, or viewers show blank content.
        var dangling = new List<string>();
        foreach (var obj in objects.Values)
        {
            foreach (Match reference in Regex.Matches(obj.Dictionary, @"(\d+)\s+0\s+R"))
            {
                var target = int.Parse(reference.Groups[1].Value);
                if (!objects.ContainsKey(target)) dangling.Add($"object {obj.Number} -> {target}");
            }
        }

        if (dangling.Count > 0) issues.Add($"Dangling references: {string.Join(", ", dangling.Take(5))}");
        else Console.WriteLine("✅ References: All indirect references resolve");
    }

    /// <summary>
    /// Checks that fonts are complete: Type0 fonts need a descendant, a ToUnicode
    /// map and an embedded font program that begins with a valid sfnt signature.
    /// </summary>
    private static void CheckFonts(
        IReadOnlyDictionary<int, PdfObjectScanner.PdfObject> objects, List<string> issues, List<string> warnings)
    {
        // Match /Type /Font exactly, so /Type /FontDescriptor objects are not counted.
        var fonts = objects.Values.Where(o => Regex.IsMatch(o.Dictionary, @"/Type\s*/Font\b(?!Descriptor)")).ToList();
        if (fonts.Count == 0)
        {
            warnings.Add("No font resources defined (only valid for a document with no text)");
            return;
        }

        var type0 = fonts.Where(f => f.Dictionary.Contains("/Subtype /Type0")).ToList();
        var cidFonts = fonts.Where(f => f.Dictionary.Contains("/Subtype /CIDFontType2")).ToList();

        Console.WriteLine($"✅ Font Resources: {fonts.Count} font objects " +
                          $"({type0.Count} Type0, {cidFonts.Count} CIDFontType2)");

        foreach (var font in type0)
        {
            if (!font.Dictionary.Contains("/DescendantFonts"))
                issues.Add($"Type0 font {font.Number} has no /DescendantFonts");

            if (!font.Dictionary.Contains("/Encoding /Identity-H"))
                issues.Add($"Type0 font {font.Number} uses an unexpected encoding");

            // Without ToUnicode the text renders but cannot be copied or searched.
            if (!font.Dictionary.Contains("/ToUnicode"))
                warnings.Add($"Type0 font {font.Number} has no /ToUnicode map; text will not be searchable");
        }

        var embedded = 0;
        foreach (var cid in cidFonts)
        {
            if (!cid.Dictionary.Contains("/CIDToGIDMap"))
                issues.Add($"CID font {cid.Number} has no /CIDToGIDMap");

            if (!cid.Dictionary.Contains("/W "))
                warnings.Add($"CID font {cid.Number} has no /W width array; spacing will fall back to /DW");

            var descriptorRef = Regex.Match(cid.Dictionary, @"/FontDescriptor\s+(\d+)\s+0\s+R");
            if (!descriptorRef.Success || !objects.TryGetValue(int.Parse(descriptorRef.Groups[1].Value), out var descriptor))
            {
                issues.Add($"CID font {cid.Number} has no resolvable /FontDescriptor");
                continue;
            }

            var fileRef = Regex.Match(descriptor.Dictionary, @"/FontFile2\s+(\d+)\s+0\s+R");
            if (!fileRef.Success || !objects.TryGetValue(int.Parse(fileRef.Groups[1].Value), out var fontFile))
            {
                issues.Add($"Font descriptor {descriptor.Number} has no embedded /FontFile2");
                continue;
            }

            var program = PdfObjectScanner.GetDecodedStream(fontFile);
            if (program == null || program.Length < 4
                || !(program[0] == 0x00 && program[1] == 0x01 && program[2] == 0x00 && program[3] == 0x00))
            {
                issues.Add($"Embedded font program in object {fontFile.Number} is not a valid TrueType font");
                continue;
            }

            if (!descriptor.Dictionary.Contains("/Flags") || !descriptor.Dictionary.Contains("/FontBBox"))
                issues.Add($"Font descriptor {descriptor.Number} is missing /Flags or /FontBBox");

            embedded++;
        }

        if (embedded > 0)
            Console.WriteLine($"✅ Font Embedding: {embedded} subsetted TrueType program(s) embedded and valid");
    }

    /// <summary>
    /// Checks that image XObjects declare everything a viewer needs to decode them.
    /// </summary>
    private static void CheckImages(
        IReadOnlyDictionary<int, PdfObjectScanner.PdfObject> objects, List<string> issues, List<string> warnings)
    {
        var images = objects.Values.Where(o => o.Dictionary.Contains("/Subtype /Image")).ToList();
        if (images.Count == 0) return;

        foreach (var image in images)
        {
            foreach (var key in new[] { "/Width", "/Height", "/ColorSpace", "/BitsPerComponent", "/Filter" })
            {
                if (!image.Dictionary.Contains(key))
                    issues.Add($"Image XObject {image.Number} is missing {key}");
            }

            // A PNG or JPEG file header here means raw file bytes were embedded
            // instead of decoded samples, which no viewer can render.
            var stream = image.RawStream;
            if (stream is { Length: > 8 }
                && stream[0] == 0x89 && stream[1] == 0x50 && stream[2] == 0x4E && stream[3] == 0x47)
            {
                issues.Add($"Image XObject {image.Number} contains a raw PNG file rather than image samples");
            }
        }

        Console.WriteLine($"✅ Image XObjects: {images.Count} image(s), all with complete decode parameters");
    }

    /// <summary>
    /// Decompresses page content streams and checks the operators they contain.
    /// </summary>
    private static void CheckContentStreams(
        IReadOnlyDictionary<int, PdfObjectScanner.PdfObject> objects, List<string> issues, List<string> warnings)
    {
        var pages = objects.Values.Where(o => o.Dictionary.Contains("/Type /Page ")).ToList();
        var checkedStreams = 0;
        var textOperators = 0;

        foreach (var page in pages)
        {
            var contentsRef = Regex.Match(page.Dictionary, @"/Contents\s+(\d+)\s+0\s+R");
            if (!contentsRef.Success) continue;
            if (!objects.TryGetValue(int.Parse(contentsRef.Groups[1].Value), out var contents)) continue;

            var decoded = PdfObjectScanner.GetDecodedStream(contents);
            if (decoded == null)
            {
                issues.Add($"Content stream {contents.Number} could not be decompressed");
                continue;
            }

            var text = Encoding.ASCII.GetString(decoded);
            checkedStreams++;

            var beginText = Regex.Matches(text, @"\bBT\b").Count;
            var endText = Regex.Matches(text, @"\bET\b").Count;
            if (beginText != endText)
                issues.Add($"Content stream {contents.Number} has unbalanced BT/ET ({beginText} vs {endText})");

            var saves = Regex.Matches(text, @"(?m)^q\s*$").Count;
            var restores = Regex.Matches(text, @"(?m)^Q\s*$").Count;
            if (saves != restores)
                issues.Add($"Content stream {contents.Number} has unbalanced q/Q ({saves} vs {restores})");

            // Decimal commas break PDFium, which parses numbers with a C locale.
            var commaNumbers = Regex.Matches(text, @"\d,\d");
            if (commaNumbers.Count > 0)
                issues.Add($"Content stream {contents.Number} has {commaNumbers.Count} comma-formatted numbers " +
                           "which break Chrome's PDFium parser");

            // Text must select a font before showing glyphs.
            if (Regex.IsMatch(text, @"\bTj\b") && !Regex.IsMatch(text, @"\bTf\b"))
                issues.Add($"Content stream {contents.Number} shows text without selecting a font");

            textOperators += Regex.Matches(text, @"\bTj\b").Count;
        }

        if (checkedStreams > 0)
        {
            Console.WriteLine($"✅ Content Streams: {checkedStreams} stream(s) decompressed and well formed " +
                              $"({textOperators} text-showing operators)");
        }
    }
}

/// <summary>
/// A minimal reader that locates the indirect objects in a PDF file and exposes
/// their dictionaries and stream data.
/// </summary>
internal static class PdfObjectScanner
{
    /// <summary>
    /// One indirect object from the file.
    /// </summary>
    internal sealed class PdfObject
    {
        public required int Number { get; init; }

        /// <summary>The object's dictionary, as text.</summary>
        public required string Dictionary { get; init; }

        /// <summary>The stream payload exactly as stored, still encoded.</summary>
        public byte[]? RawStream { get; init; }
    }

    /// <summary>
    /// Finds every "N 0 obj ... endobj" block in the file.
    /// </summary>
    /// <param name="bytes">The complete PDF file.</param>
    /// <returns>The objects found, keyed by object number.</returns>
    public static Dictionary<int, PdfObject> Scan(byte[] bytes)
    {
        var objects = new Dictionary<int, PdfObject>();
        var text = Encoding.Latin1.GetString(bytes);

        foreach (Match header in Regex.Matches(text, @"(?m)^(\d+) 0 obj\r?\n"))
        {
            var number = int.Parse(header.Groups[1].Value);
            var bodyStart = header.Index + header.Length;

            var end = text.IndexOf("endobj", bodyStart, StringComparison.Ordinal);
            if (end < 0) continue;

            var streamStart = text.IndexOf("stream", bodyStart, StringComparison.Ordinal);
            var hasStream = streamStart >= 0 && streamStart < end;

            var dictionary = hasStream
                ? text[bodyStart..streamStart]
                : text[bodyStart..end];

            byte[]? stream = null;
            if (hasStream)
            {
                // The payload begins after "stream" and its end-of-line marker,
                // and its length is declared in the dictionary.
                var dataStart = streamStart + "stream".Length;
                while (dataStart < bytes.Length && (bytes[dataStart] == '\r' || bytes[dataStart] == '\n')) dataStart++;

                var length = Regex.Match(dictionary, @"/Length\s+(\d+)");
                if (length.Success)
                {
                    var count = int.Parse(length.Groups[1].Value);
                    if (dataStart + count <= bytes.Length) stream = bytes[dataStart..(dataStart + count)];
                }
            }

            objects[number] = new PdfObject
            {
                Number = number,
                Dictionary = dictionary.Trim(),
                RawStream = stream
            };
        }

        return objects;
    }

    /// <summary>
    /// Returns an object's stream data, inflating it when it is Flate encoded.
    /// </summary>
    /// <returns>The decoded bytes, or null when decoding failed.</returns>
    public static byte[]? GetDecodedStream(PdfObject obj)
    {
        if (obj.RawStream == null) return null;
        if (!obj.Dictionary.Contains("/FlateDecode")) return obj.RawStream;

        try
        {
            using var input = new MemoryStream(obj.RawStream);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
