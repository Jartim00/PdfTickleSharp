using System.Globalization;
using System.Text;
using PdfTickleSharp.Core.Document;

namespace PdfTickleSharp.Core.IO;

/// <summary>
/// Writes PDF documents to files or streams in basic PDF format.
/// </summary>
public class PdfWriter
{
    private readonly List<PdfObject> _objects;
    private int _nextObjectNumber;

    /// <summary>
    /// Initializes a new instance of the PdfWriter class.
    /// </summary>
    public PdfWriter()
    {
        _objects = new List<PdfObject>();
        _nextObjectNumber = 1;
    }

    /// <summary>
    /// Writes the PDF document to the specified file path.
    /// </summary>
    /// <param name="document">The PDF document to write.</param>
    /// <param name="filePath">The file path to write to.</param>
    public void WriteToFile(PdfDocument document, string filePath)
    {
        var pdfBytes = GeneratePdf(document);
        File.WriteAllBytes(filePath, pdfBytes);
    }

    /// <summary>
    /// Writes the PDF document to the specified stream.
    /// </summary>
    /// <param name="document">The PDF document to write.</param>
    /// <param name="stream">The stream to write to.</param>
    public void WriteToStream(PdfDocument document, Stream stream)
    {
        var pdfBytes = GeneratePdf(document);
        stream.Write(pdfBytes, 0, pdfBytes.Length);
    }

    /// <summary>
    /// Generates the PDF file content as a byte array.
    /// </summary>
    /// <param name="document">The PDF document to generate.</param>
    /// <returns>The PDF file content as bytes.</returns>
    public byte[] GeneratePdf(PdfDocument document)
    {
        _objects.Clear();
        _nextObjectNumber = 1;

        var output = new StringBuilder();

        // PDF Header
        output.AppendLine("%PDF-1.4");
        output.AppendLine("%ÂÃÄÅ"); // Binary comment for compatibility

        // Create document structure
        var catalog = CreateCatalog();
        var pagesRoot = CreatePagesRoot(document);
        var pageObjects = CreatePageObjects(document);

        // Update pages root with page references
        pagesRoot.Content = pagesRoot.Content.Replace("PAGES_PLACEHOLDER", 
            string.Join(" ", pageObjects.Select(p => $"{p.Number} 0 R")));
        pagesRoot.Content = pagesRoot.Content.Replace("COUNT_PLACEHOLDER", 
            document.PageCount.ToString());

        // Write all objects
        var xrefOffsets = new List<long>();
        foreach (var obj in _objects)
        {
            xrefOffsets.Add(output.Length);
            output.AppendLine($"{obj.Number} 0 obj");
            output.AppendLine(obj.Content);
            output.AppendLine("endobj");
            output.AppendLine();
        }

        // Cross-reference table
        var xrefStart = output.Length;
        output.AppendLine("xref");
        output.AppendLine($"0 {_objects.Count + 1}");
        output.AppendLine("0000000000 65535 f ");
        
        foreach (var offset in xrefOffsets)
        {
            output.AppendLine($"{offset:D10} 00000 n ");
        }

        // Trailer
        output.AppendLine("trailer");
        output.AppendLine("<<");
        output.AppendLine($"/Size {_objects.Count + 1}");
        output.AppendLine($"/Root {catalog.Number} 0 R");
        output.AppendLine(">>");
        output.AppendLine("startxref");
        output.AppendLine(xrefStart.ToString());
        output.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private PdfObject CreateCatalog()
    {
        var catalog = new PdfObject(_nextObjectNumber++);
        catalog.Content = @"<<
/Type /Catalog
/Pages 2 0 R
>>";
        _objects.Add(catalog);
        return catalog;
    }

    private PdfObject CreatePagesRoot(PdfDocument document)
    {
        var pagesRoot = new PdfObject(_nextObjectNumber++);
        pagesRoot.Content = @"<<
/Type /Pages
/Kids [PAGES_PLACEHOLDER]
/Count COUNT_PLACEHOLDER
>>";
        _objects.Add(pagesRoot);
        return pagesRoot;
    }

    private List<PdfObject> CreatePageObjects(PdfDocument document)
    {
        var pageObjects = new List<PdfObject>();

        foreach (var page in document.Pages)
        {
            // Create content stream for this page
            var contentStream = CreateContentStream(page);
            
            // Create page object
            var pageObj = new PdfObject(_nextObjectNumber++);
            pageObj.Content = $@"<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 {page.PageSize.Width.ToString("F2", CultureInfo.InvariantCulture)} {page.PageSize.Height.ToString("F2", CultureInfo.InvariantCulture)}]
/Contents {contentStream.Number} 0 R
/Resources <<
  /Font <<
    /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>
  >>
>>
>>";
            _objects.Add(pageObj);
            pageObjects.Add(pageObj);
        }

        return pageObjects;
    }

    private PdfObject CreateContentStream(PdfPage page)
    {
        var content = new StringBuilder();
        content.AppendLine("BT"); // Begin text
        content.AppendLine("/F1 12 Tf"); // Set font

        // Add all text content from the page
        foreach (var textItem in GetTextContent(page))
        {
            var x = textItem.X.ToString("F2", CultureInfo.InvariantCulture);
            var y = textItem.Y.ToString("F2", CultureInfo.InvariantCulture);
            var text = EscapePdfString(textItem.Text);
            
            content.AppendLine($"{x} {y} Td"); // Move to position
            content.AppendLine($"({text}) Tj"); // Show text
            content.AppendLine($"{-textItem.X} {-textItem.Y} Td"); // Reset position
        }

        content.AppendLine("ET"); // End text

        var contentStr = content.ToString();
        var contentObj = new PdfObject(_nextObjectNumber++);
        contentObj.Content = $@"<<
/Length {contentStr.Length}
>>
stream
{contentStr}endstream";

        _objects.Add(contentObj);
        return contentObj;
    }

    private IEnumerable<(double X, double Y, string Text)> GetTextContent(PdfPage page)
    {
        // Parse the simple string-based content storage
        var textItems = new List<(double, double, string)>();
        
        // Access the private _contents field through reflection (for this basic implementation)
        var contentsField = typeof(PdfPage).GetField("_contents", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (contentsField?.GetValue(page) is List<string> contents)
        {
            foreach (var content in contents)
            {
                // Parse "Text: 'Hello World!' at (100, 700)" format
                if (content.StartsWith("Text: '") && content.Contains("' at ("))
                {
                    var textStart = content.IndexOf('\'') + 1;
                    var textEnd = content.IndexOf("' at (");
                    var coordStart = content.IndexOf("(", textEnd) + 1;
                    var coordEnd = content.IndexOf(")", coordStart);
                    
                    if (textStart > 0 && textEnd > textStart && coordStart > 0 && coordEnd > coordStart)
                    {
                        var text = content.Substring(textStart, textEnd - textStart);
                        var coords = content.Substring(coordStart, coordEnd - coordStart).Split(", ");
                        
                        if (coords.Length == 2 && 
                            double.TryParse(coords[0], out var x) && 
                            double.TryParse(coords[1], out var y))
                        {
                            textItems.Add((x, y, text));
                        }
                    }
                }
            }
        }
        
        return textItems;
    }

    private static string EscapePdfString(string text)
    {
        return text.Replace("\\", "\\\\")
                  .Replace("(", "\\(")
                  .Replace(")", "\\)")
                  .Replace("\r", "\\r")
                  .Replace("\n", "\\n")
                  .Replace("\t", "\\t");
    }

    /// <summary>
    /// Represents a PDF object with a number and content.
    /// </summary>
    private class PdfObject
    {
        public int Number { get; }
        public string Content { get; set; }

        public PdfObject(int number)
        {
            Number = number;
            Content = string.Empty;
        }
    }
} 