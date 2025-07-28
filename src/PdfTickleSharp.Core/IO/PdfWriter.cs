using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PdfTickleSharp.Core.Document;
using PdfTickleSharp.Core.Graphics;
using PdfTickleSharp.Core.Text;

namespace PdfTickleSharp.Core.IO
{
    /// <summary>
    /// Writes PDF documents to files or streams in basic PDF format.
    /// </summary>
    public class PdfWriter
    {
        private readonly List<PdfObject> _objects = new();
        private int _nextObjectNumber = 1;

        /// <summary>
        /// Writes the PDF document to the specified file path.
        /// </summary>
        public void WriteToFile(PdfDocument document, string filePath)
        {
            File.WriteAllBytes(filePath, GeneratePdf(document));
        }

        /// <summary>
        /// Writes the PDF document to the specified stream.
        /// </summary>
        public void WriteToStream(PdfDocument document, Stream stream)
        {
            stream.Write(GeneratePdf(document));
        }

        /// <summary>
        /// Generates the PDF file content as a byte array.
        /// </summary>
        public byte[] GeneratePdf(PdfDocument document)
        {
            _objects.Clear();
            _nextObjectNumber = 1;

            // Build structure
            var catalog = CreateCatalog();
            var pagesRoot = CreatePagesRoot(document);
            var pageObjects = CreatePageObjects(document);
            pagesRoot.Content = pagesRoot.Content
                .Replace("PAGES_PLACEHOLDER", string.Join(" ", pageObjects.Select(p => $"{p.Number} 0 R")))
                .Replace("COUNT_PLACEHOLDER", document.PageCount.ToString());

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)
            {
                AutoFlush = true
            };

            // PDF header
            writer.WriteLine("%PDF-1.4");
            writer.WriteLine("%âãÏÓ"); // binary comment for compatibility

            // Write objects
            var offsets = new List<long>();
            foreach (var obj in _objects)
            {
                offsets.Add(ms.Position);
                writer.WriteLine($"{obj.Number} 0 obj");
                writer.WriteLine(obj.Content);
                writer.WriteLine("endobj");
                writer.WriteLine();
            }

            // Cross-reference
            var xrefStart = ms.Position;
            writer.WriteLine("xref");
            writer.WriteLine($"0 {_objects.Count + 1}");
            writer.WriteLine("0000000000 65535 f ");
            foreach (var off in offsets)
                writer.WriteLine($"{off:D10} 00000 n ");

            // Trailer
            writer.WriteLine("trailer");
            writer.WriteLine("<<");
            writer.WriteLine($"/Size {_objects.Count + 1}");
            writer.WriteLine($"/Root {catalog.Number} 0 R");
            writer.WriteLine(">>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefStart.ToString());
            writer.WriteLine("%%EOF");

            writer.Flush();
            return ms.ToArray();
        }

        private PdfObject CreateCatalog()
        {
            var obj = new PdfObject(_nextObjectNumber++)
            {
                Content = @"<<
/Type /Catalog
/Pages 2 0 R
>>"
            };
            _objects.Add(obj);
            return obj;
        }

        private PdfObject CreatePagesRoot(PdfDocument document)
        {
            var obj = new PdfObject(_nextObjectNumber++)
            {
                Content = @"<<
/Type /Pages
/Kids [PAGES_PLACEHOLDER]
/Count COUNT_PLACEHOLDER
>>"
            };
            _objects.Add(obj);
            return obj;
        }

        private List<PdfObject> CreatePageObjects(PdfDocument document)
        {
            var list = new List<PdfObject>();
            foreach (var page in document.Pages)
            {
                var contentStream = CreateContentStream(page);
                var obj = new PdfObject(_nextObjectNumber++)
                {
                    Content = $@"<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 {page.PageSize.Width:F2} {page.PageSize.Height:F2}]
/Contents {contentStream.Number} 0 R
/Resources <<
  /Font <<
    {string.Join("\n    ", GetFontResources())}
  >>
  /ColorSpace <<
    /DeviceRGB << /Type /ColorSpace /Subtype /DeviceRGB >>
  >>
>>"
                };
                _objects.Add(obj);
                list.Add(obj);
            }
            return list;
        }

        private PdfObject CreateContentStream(PdfPage page)
        {
            var sb = new StringBuilder();

            // Text content
            foreach (var t in GetTextContent(page))
            {
                sb.AppendLine("BT");
                sb.AppendLine($"/{GetFontName(t.Format)} {t.Format.FontSize} Tf");
                sb.AppendLine($"{FormatColor(t.Format.Color)} rg");
                sb.AppendLine("0 Tr");
                sb.AppendLine($"{FormatCoordinate(t.X, t.Y)} Td");
                sb.AppendLine($"({EscapePdfString(t.Text)}) Tj");
                sb.AppendLine("ET");
            }

            // Text flow
            foreach (var f in GetTextFlowContent(page))
            {
                sb.AppendLine("BT");
                sb.AppendLine($"/{GetFontName(f.Format)} {f.Format.FontSize} Tf");
                sb.AppendLine($"{FormatColor(f.Format.Color)} rg");
                sb.AppendLine("0 Tr");
                sb.AppendLine($"{FormatCoordinate(f.X, f.Y)} Td");
                sb.AppendLine($"({EscapePdfString(f.Text)}) Tj");
                sb.AppendLine("ET");
            }

            // Lines
            foreach (var l in GetLineContent(page))
            {
                sb.AppendLine("/DeviceRGB CS");
                sb.AppendLine($"{FormatColor(l.Color)} RG");
                sb.AppendLine($"{l.Width} w");
                sb.AppendLine($"{FormatCoordinate(l.X1, l.Y1)} m");
                sb.AppendLine($"{FormatCoordinate(l.X2, l.Y2)} l");
                sb.AppendLine("S");
            }

            // Rectangles
            foreach (var r in GetRectangleContent(page))
            {
                if (r.Filled)
                {
                    sb.AppendLine("/DeviceRGB cs");
                    sb.AppendLine($"{FormatColor(r.Color)} rg");
                }
                else
                {
                    sb.AppendLine("/DeviceRGB CS");
                    sb.AppendLine($"{FormatColor(r.Color)} RG");
                }
                sb.AppendLine($"{r.LineWidth} w");
                sb.AppendLine($"{FormatCoordinate(r.X, r.Y)} {r.Width:F2} {r.Height:F2} re");
                sb.AppendLine(r.Filled ? "f" : "S");
            }

            // Circles
            foreach (var c in GetCircleContent(page))
            {
                if (c.Filled)
                {
                    sb.AppendLine("/DeviceRGB cs");
                    sb.AppendLine($"{FormatColor(c.Color)} rg");
                }
                else
                {
                    sb.AppendLine("/DeviceRGB CS");
                    sb.AppendLine($"{FormatColor(c.Color)} RG");
                }
                sb.AppendLine($"{c.LineWidth} w");
                DrawCircle(sb, c.CenterX, c.CenterY, c.Radius);
                sb.AppendLine(c.Filled ? "f" : "S");
            }

            // Image placeholders
            foreach (var i in GetImageContent(page))
            {
                sb.AppendLine("0.8 0.8 0.8 rg");
                sb.AppendLine($"{i.X:F2} {i.Y:F2} {i.Width:F2} {i.Height:F2} re");
                sb.AppendLine("f");
            }

            var contentStr = sb.ToString();
            var contentBytes = Encoding.ASCII.GetBytes(contentStr);
            var obj = new PdfObject(_nextObjectNumber++)
            {
                Content = $@"<<
/Length {contentBytes.Length}
>>
stream
{contentStr}endstream"};
            _objects.Add(obj);
            return obj;
        }

        private void DrawCircle(StringBuilder sb, double cx, double cy, double r)
        {
            const double k = 0.5522848;
            // Use the original coordinate system for now - circles are less critical
            sb.AppendLine($"{(cx + r):F2} {cy:F2} m");
            sb.AppendLine($"{(cx + r):F2} {(cy + k*r):F2} {(cx + k*r):F2} {(cy + r):F2} {cx:F2} {(cy + r):F2} c");
            sb.AppendLine($"{(cx - k*r):F2} {(cy + r):F2} {(cx - r):F2} {(cy + k*r):F2} {(cx - r):F2} {cy:F2} c");
            sb.AppendLine($"{(cx - r):F2} {(cy - k*r):F2} {(cx - k*r):F2} {(cy - r):F2} {cx:F2} {(cy - r):F2} c");
            sb.AppendLine($"{(cx + k*r):F2} {(cy - r):F2} {(cx + r):F2} {(cy - k*r):F2} {(cx + r):F2} {cy:F2} c");
        }

        private IEnumerable<string> GetFontResources()
            => new[] { "Helvetica", "Helvetica-Bold", "Helvetica-Italic", "Helvetica-BoldItalic",
                       "Times", "Times-Bold", "Times-Italic", "Times-BoldItalic",
                       "Courier", "Courier-Bold", "Courier-Italic", "Courier-BoldItalic" }
                .Select(f => $"/{f} << /Type /Font /Subtype /Type1 /BaseFont /{f} >>");

        private IEnumerable<(double X, double Y, string Text, TextFormat Format)> GetTextContent(PdfPage page)
            => page.GetContents().OfType<TextContent>()
                   .Select(tc => (tc.X, tc.Y, tc.Text, tc.Format));

        private IEnumerable<(double X, double Y, string Text, double Width, TextFormat Format)> GetTextFlowContent(PdfPage page)
            => page.GetContents().OfType<TextFlowContent>()
                   .Select(tf => (tf.X, tf.Y, tf.Text, tf.Width, tf.Format));

        private IEnumerable<(double X1, double Y1, double X2, double Y2, Color Color, double Width)> GetLineContent(PdfPage page)
            => page.GetContents().OfType<LineContent>()
                   .Select(l => (l.X1, l.Y1, l.X2, l.Y2, l.Color, l.Width));

        private IEnumerable<(double X, double Y, double Width, double Height, Color Color, double LineWidth, bool Filled)> GetRectangleContent(PdfPage page)
            => page.GetContents().OfType<RectangleContent>()
                   .Select(r => (r.X, r.Y, r.Width, r.Height, r.Color, r.LineWidth, r.Filled));

        private IEnumerable<(double CenterX, double CenterY, double Radius, Color Color, double LineWidth, bool Filled)> GetCircleContent(PdfPage page)
            => page.GetContents().OfType<CircleContent>()
                   .Select(c => (c.CenterX, c.CenterY, c.Radius, c.Color, c.LineWidth, c.Filled));

        private IEnumerable<(byte[] ImageData, double X, double Y, double Width, double Height)> GetImageContent(PdfPage page)
            => page.GetContents().OfType<ImageContent>()
                   .Select(i => (i.ImageData, i.X, i.Y, i.Width, i.Height));

        private static string GetFontName(TextFormat format)
        {
            var suffix = format.IsBold && format.IsItalic ? "-BoldItalic"
                       : format.IsBold ? "-Bold"
                       : format.IsItalic ? "-Italic"
                       : string.Empty;
            return format.FontFamily + suffix;
        }

        private static string EscapePdfString(string text)
        {
            // Check if the text contains any non-ASCII characters
            bool hasUnicode = text.Any(c => c >= 128);
            
            if (!hasUnicode)
            {
                // Pure ASCII - use simple escaping
                return text.Replace("\\", "\\\\").Replace("(", "\\(")
                          .Replace(")", "\\)").Replace("\r", "\\r")
                          .Replace("\n", "\\n").Replace("\t", "\\t");
            }
            
            // For Unicode text, we'll use a simpler approach that's more compatible
            // with PDF viewers by replacing problematic characters
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c < 128)
                {
                    // ASCII characters - escape special PDF characters
                    switch (c)
                    {
                        case '\\': sb.Append("\\\\"); break;
                        case '(': sb.Append("\\("); break;
                        case ')': sb.Append("\\)"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\t': sb.Append("\\t"); break;
                        default: sb.Append(c); break;
                    }
                }
                else
                {
                    // Replace Unicode characters with ASCII equivalents or fallback
                    switch (c)
                    {
                        case 'é': sb.Append("e"); break;
                        case 'è': sb.Append("e"); break;
                        case 'ë': sb.Append("e"); break;
                        case 'à': sb.Append("a"); break;
                        case 'â': sb.Append("a"); break;
                        case 'ä': sb.Append("a"); break;
                        case 'ù': sb.Append("u"); break;
                        case 'û': sb.Append("u"); break;
                        case 'ü': sb.Append("u"); break;
                        case 'ô': sb.Append("o"); break;
                        case 'ö': sb.Append("o"); break;
                        case 'î': sb.Append("i"); break;
                        case 'ï': sb.Append("i"); break;
                        case 'ç': sb.Append("c"); break;
                        case 'ñ': sb.Append("n"); break;
                        case '°': sb.Append(" degrees"); break;
                        case '™': sb.Append("(TM)"); break;
                        case '©': sb.Append("(C)"); break;
                        case '®': sb.Append("(R)"); break;
                        default: sb.Append('?'); break;
                    }
                }
            }
            return sb.ToString();
        }

        private static string FormatColor(Color color)
        {
            // PDFium compatibility: use decimal format instead of comma format
            return $"{color.NormalizedR:F1} {color.NormalizedG:F1} {color.NormalizedB:F1}";
        }

        private static string FormatCoordinate(double x, double y, double pageHeight = 841.89)
        {
            // PDFium compatibility: ensure coordinates are properly formatted
            // Y-coordinate needs to be flipped for PDFium compatibility
            var flippedY = pageHeight - y;
            return $"{x:F2} {flippedY:F2}";
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
}
