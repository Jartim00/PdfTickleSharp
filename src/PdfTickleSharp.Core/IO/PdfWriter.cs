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
        private readonly Dictionary<byte[], int> _imageXObjects = new();

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
            writer.WriteLine("%âäÜÒ"); // binary comment for compatibility

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
                var imageResources = GetImageResourcesForPage(page);
                
                var obj = new PdfObject(_nextObjectNumber++)
                {
                    Content = $@"<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 {page.PageSize.Width.ToString("F2", CultureInfo.InvariantCulture)} {page.PageSize.Height.ToString("F2", CultureInfo.InvariantCulture)}]
/Contents {contentStream.Number} 0 R
/Resources <<
  /Font <<
    {string.Join("\n    ", GetFontResources())}
  >>
  /ColorSpace <<
    /DeviceRGB << /Type /ColorSpace /Subtype /DeviceRGB >>
  >>
{(imageResources.Any() ? $"  /XObject <<\n    {string.Join("\n    ", imageResources)}\n  >>" : "")}
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
                var adjustedX = AdjustXForAlignment(t.X, t.Text, t.Format, page.PageSize.Width);
                sb.AppendLine("BT");
                sb.AppendLine($"/{GetFontName(t.Format)} {t.Format.FontSize.ToString(CultureInfo.InvariantCulture)} Tf");
                sb.AppendLine($"{FormatColor(t.Format.Color)} rg");
                sb.AppendLine("0 Tr");
                sb.AppendLine($"{FormatCoordinate(adjustedX, t.Y, page.PageSize.Height)} Td");
                var escapedText = EscapePdfString(t.Text);
                if (escapedText.StartsWith("<") && escapedText.EndsWith(">"))
                {
                    // Hex string - no parentheses needed
                    sb.AppendLine($"{escapedText} Tj");
                }
                else
                {
                    // Regular string - use parentheses
                    sb.AppendLine($"({escapedText}) Tj");
                }
                sb.AppendLine("ET");
            }

            // Text flow
            foreach (var f in GetTextFlowContent(page))
            {
                var lines = WrapText(f.Text, f.Width, f.Format);
                var currentY = f.Y;
                var lineHeight = f.Format.FontSize * f.Format.LineSpacing;
                
                foreach (var line in lines)
                {
                    sb.AppendLine("BT");
                    sb.AppendLine($"/{GetFontName(f.Format)} {f.Format.FontSize.ToString(CultureInfo.InvariantCulture)} Tf");
                    sb.AppendLine($"{FormatColor(f.Format.Color)} rg");
                    sb.AppendLine("0 Tr");
                    sb.AppendLine($"{FormatCoordinate(f.X, currentY, page.PageSize.Height)} Td");
                    var escapedText = EscapePdfString(line);
                    if (escapedText.StartsWith("<") && escapedText.EndsWith(">"))
                    {
                        // Hex string - no parentheses needed
                        sb.AppendLine($"{escapedText} Tj");
                    }
                    else
                    {
                        // Regular string - use parentheses
                        sb.AppendLine($"({escapedText}) Tj");
                    }
                    sb.AppendLine("ET");
                    
                    currentY -= lineHeight; // Move to next line
                }
            }

            // Lines
            foreach (var l in GetLineContent(page))
            {
                sb.AppendLine("/DeviceRGB CS");
                sb.AppendLine($"{FormatColor(l.Color)} RG");
                sb.AppendLine($"{l.Width.ToString(CultureInfo.InvariantCulture)} w");
                sb.AppendLine($"{FormatCoordinate(l.X1, l.Y1, page.PageSize.Height)} m");
                sb.AppendLine($"{FormatCoordinate(l.X2, l.Y2, page.PageSize.Height)} l");
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
                sb.AppendLine($"{r.LineWidth.ToString(CultureInfo.InvariantCulture)} w");
                sb.AppendLine($"{FormatCoordinate(r.X, r.Y, page.PageSize.Height)} {r.Width.ToString("F2", CultureInfo.InvariantCulture)} {r.Height.ToString("F2", CultureInfo.InvariantCulture)} re");
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
                sb.AppendLine($"{c.LineWidth.ToString(CultureInfo.InvariantCulture)} w");
                DrawCircle(sb, c.CenterX, c.CenterY, c.Radius, page.PageSize.Height);
                sb.AppendLine(c.Filled ? "f" : "S");
            }

            // Images
            foreach (var i in GetImageContent(page))
            {
                var imageId = CreateImageXObject(i.ImageData);
                sb.AppendLine("q"); // Save graphics state
                sb.AppendLine($"{i.Width.ToString("F2", CultureInfo.InvariantCulture)} 0 0 {i.Height.ToString("F2", CultureInfo.InvariantCulture)} {FormatCoordinate(i.X, i.Y, page.PageSize.Height)} cm");
                sb.AppendLine($"/Im{imageId} Do");
                sb.AppendLine("Q"); // Restore graphics state
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

        private int CreateImageXObject(byte[] imageData)
        {
            if (_imageXObjects.TryGetValue(imageData, out int existingId))
            {
                return existingId;
            }

            var objNumber = _nextObjectNumber++;

            var obj = new PdfObject(objNumber)
            {
                Content = $@"<<
/Type /XObject
/Subtype /Image
/Width 1
/Height 1
/ColorSpace /DeviceRGB
/BitsPerComponent 8
/Length {imageData.Length}
>>
stream
{Convert.ToBase64String(imageData)}
endstream"
            };

            _objects.Add(obj);
            _imageXObjects[imageData] = objNumber;

            return objNumber;
        }

        private List<string> GetImageResourcesForPage(PdfPage page)
        {
            var xObjects = new List<string>();
            foreach (var image in GetImageContent(page))
            {
                var id = CreateImageXObject(image.ImageData);
                xObjects.Add($"/Im{id} {id} 0 R");
            }
            return xObjects;
        }

        private void DrawCircle(StringBuilder sb, double cx, double cy, double r, double pageHeight)
        {
            const double k = 0.5522848;
            
            // Use the coordinate system as-is since we're not flipping Y anymore
            sb.AppendLine($"{cx.ToString("F2", CultureInfo.InvariantCulture)} {(cy + r).ToString("F2", CultureInfo.InvariantCulture)} m");
            sb.AppendLine($"{(cx + k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cy + r).ToString("F2", CultureInfo.InvariantCulture)} {(cx + r).ToString("F2", CultureInfo.InvariantCulture)} {(cy + k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cx + r).ToString("F2", CultureInfo.InvariantCulture)} {cy.ToString("F2", CultureInfo.InvariantCulture)} c");
            sb.AppendLine($"{(cx + r).ToString("F2", CultureInfo.InvariantCulture)} {(cy - k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cx + k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cy - r).ToString("F2", CultureInfo.InvariantCulture)} {cx.ToString("F2", CultureInfo.InvariantCulture)} {(cy - r).ToString("F2", CultureInfo.InvariantCulture)} c");
            sb.AppendLine($"{(cx - k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cy - r).ToString("F2", CultureInfo.InvariantCulture)} {(cx - r).ToString("F2", CultureInfo.InvariantCulture)} {(cy - k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cx - r).ToString("F2", CultureInfo.InvariantCulture)} {cy.ToString("F2", CultureInfo.InvariantCulture)} c");
            sb.AppendLine($"{(cx - r).ToString("F2", CultureInfo.InvariantCulture)} {(cy + k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cx - k*r).ToString("F2", CultureInfo.InvariantCulture)} {(cy + r).ToString("F2", CultureInfo.InvariantCulture)} {cx.ToString("F2", CultureInfo.InvariantCulture)} {(cy + r).ToString("F2", CultureInfo.InvariantCulture)} c");
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
            // Replace characters that standard PDF fonts can't display with readable alternatives
            var processedText = ProcessUnicodeCharacters(text);
            
            // Check if the processed text still contains any non-ASCII characters
            bool hasUnicode = processedText.Any(c => c >= 128);
            
            if (!hasUnicode)
            {
                // Pure ASCII - use simple escaping in parentheses
                var sb = new StringBuilder();
                foreach (char c in processedText)
                {
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
                return sb.ToString();
            }
            else
            {
                // For remaining Unicode characters (mostly Latin-1 supplement), use hex encoding
                var utf16Bytes = Encoding.BigEndianUnicode.GetBytes(processedText);
                var hexSb = new StringBuilder();
                
                foreach (byte b in utf16Bytes)
                {
                    hexSb.Append($"{b:X2}");
                }
                
                return $"<{hexSb}>";
            }
        }
        
        private static string ProcessUnicodeCharacters(string text)
        {
            var sb = new StringBuilder();
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                // Handle surrogate pairs (for emojis and other high Unicode characters)
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    var codePoint = char.ConvertToUtf32(c, text[i + 1]);
                    sb.Append(GetUnicodeReplacement(codePoint));
                    i++; // Skip the low surrogate
                }
                else if (c > 255) // Characters beyond Latin-1
                {
                    sb.Append(GetUnicodeReplacement(c));
                }
                else if (c >= 128 && c <= 255) // Latin-1 supplement - keep as is for hex encoding
                {
                    sb.Append(c);
                }
                else // ASCII characters
                {
                    sb.Append(c);
                }
            }
            
            return sb.ToString();
        }
        
        private static string GetUnicodeReplacement(int codePoint)
        {
            // Common emoji replacements
            return codePoint switch
            {
                0x2728 => "[sparkles]",
                0x1F389 => "[party]",
                0x1F680 => "[rocket]",
                0x1F4C4 => "[document]",
                0x1F31F => "[star]",
                0x1F3AF => "[target]",
                0x2603 => "[snowman]",
                0x2744 => "[snowflake]",
                0x2122 => "[TM]",
                0x2021 => "[dagger]",
                0x2020 => "[cross]",
                0x1F602 => "[laughing]",
                0x1F600 => ":)",
                _ when codePoint >= 0x1F600 && codePoint <= 0x1F64F => "[emoji]", // Emoticons
                _ when codePoint >= 0x1F300 && codePoint <= 0x1F5FF => "[symbol]", // Misc symbols
                _ when codePoint >= 0x1F680 && codePoint <= 0x1F6FF => "[transport]", // Transport symbols
                _ when codePoint >= 0x2600 && codePoint <= 0x26FF => "[misc]", // Miscellaneous symbols
                _ => $"[U+{codePoint:X4}]"
            };
        }

        private static string FormatColor(Color color)
        {
            // PDFium compatibility: use decimal format with dot separator
            return $"{color.NormalizedR.ToString("F1", CultureInfo.InvariantCulture)} {color.NormalizedG.ToString("F1", CultureInfo.InvariantCulture)} {color.NormalizedB.ToString("F1", CultureInfo.InvariantCulture)}";
        }

        private static string FormatCoordinate(double x, double y, double pageHeight)
        {
            // PDFium compatibility: ensure coordinates are properly formatted
            // The input coordinates are already in the correct PDF coordinate system (Y=0 at bottom)
            return $"{x.ToString("F2", CultureInfo.InvariantCulture)} {y.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        
        private static double AdjustXForAlignment(double x, string text, TextFormat format, double pageWidth)
        {
            if (format.Alignment == TextAlignment.Left)
                return x;
                
            // Estimate text width (rough approximation)
            var estimatedTextWidth = EstimateTextWidth(text, format);
            
            return format.Alignment switch
            {
                TextAlignment.Center => x - (estimatedTextWidth / 2),
                TextAlignment.Right => x - estimatedTextWidth,
                _ => x
            };
        }
        
        private static double EstimateTextWidth(string text, TextFormat format)
        {
            // Rough approximation: average character width is about 0.6 * font size
            // This is an approximation and could be improved with proper font metrics
            return text.Length * format.FontSize * 0.6;
        }
        
        private static List<string> WrapText(string text, double maxWidth, TextFormat format)
        {
            var lines = new List<string>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new StringBuilder();
            
            foreach (var word in words)
            {
                var testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                var testWidth = EstimateTextWidth(testLine, format);
                
                if (testWidth <= maxWidth)
                {
                    if (currentLine.Length > 0)
                        currentLine.Append(" ");
                    currentLine.Append(word);
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                    currentLine.Append(word);
                }
            }
            
            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());
                
            return lines.Count > 0 ? lines : new List<string> { text };
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
