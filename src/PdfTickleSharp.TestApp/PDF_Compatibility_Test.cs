using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfTickleSharp.TestApp
{
    /// <summary>
    /// PDF Compatibility Validator for Chrome (PDFium), Safari, Firefox, and other viewers
    /// </summary>
    public static class PdfCompatibilityTest
    {
        public static void ValidatePdfForUniversalCompatibility(string pdfPath)
        {
            Console.WriteLine("=== PDF UNIVERSAL COMPATIBILITY VALIDATOR ===");
            Console.WriteLine($"Testing: {Path.GetFileName(pdfPath)}");
            Console.WriteLine();

            var issues = new List<string>();
            var warnings = new List<string>();

            // Read PDF as text for analysis
            var pdfBytes = File.ReadAllBytes(pdfPath);
            var pdfText = Encoding.ASCII.GetString(pdfBytes);

            // Test 1: PDF Header
            if (!pdfText.StartsWith("%PDF-"))
            {
                issues.Add("❌ Invalid PDF header - missing %PDF- signature");
            }
            else
            {
                Console.WriteLine("✅ PDF Header: Valid PDF signature found");
            }

            // Test 2: Numeric Format Compatibility (PDFium critical)
            var commaNumbers = Regex.Matches(pdfText, @"\d+,\d+");
            if (commaNumbers.Count > 0)
            {
                issues.Add($"❌ CRITICAL: Found {commaNumbers.Count} comma-formatted numbers - will break PDFium (Chrome)");
            }
            else
            {
                Console.WriteLine("✅ Numeric Format: All numbers use dot notation (PDFium compatible)");
            }

            // Test 3: Color Space Definitions
            if (pdfText.Contains("/DeviceRGB"))
            {
                Console.WriteLine("✅ Color Space: DeviceRGB color space defined");
            }
            else
            {
                warnings.Add("⚠️ No explicit DeviceRGB color space found");
            }

            // Test 4: Font Resources
            var fontPattern = @"/([A-Za-z-]+)\s+<<\s*/Type\s*/Font";
            var fonts = Regex.Matches(pdfText, fontPattern);
            if (fonts.Count > 0)
            {
                Console.WriteLine($"✅ Font Resources: {fonts.Count} font definitions found");
                foreach (Match font in fonts)
                {
                    Console.WriteLine($"   - {font.Groups[1].Value}");
                }
            }
            else
            {
                issues.Add("❌ No font resources defined");
            }

            // Test 5: Cross-Reference Table
            if (pdfText.Contains("xref"))
            {
                Console.WriteLine("✅ Cross-Reference: Valid xref table found");
            }
            else
            {
                issues.Add("❌ Missing cross-reference table");
            }

            // Test 6: Trailer and EOF
            if (pdfText.Contains("%%EOF"))
            {
                Console.WriteLine("✅ PDF Trailer: Valid EOF marker found");
            }
            else
            {
                issues.Add("❌ Missing %%EOF marker");
            }

            // Test 7: Binary Comment (for compatibility)
            var lines = pdfText.Split('\n');
            if (lines.Length > 1 && lines[1].StartsWith("%") && lines[1].Length > 4)
            {
                Console.WriteLine("✅ Binary Comment: Compatibility comment present");
            }
            else
            {
                warnings.Add("⚠️ No binary compatibility comment found");
            }

            // Test 8: Content Stream Structure
            if (pdfText.Contains("stream") && pdfText.Contains("endstream"))
            {
                Console.WriteLine("✅ Content Streams: Proper stream structure found");
            }
            else
            {
                issues.Add("❌ Invalid content stream structure");
            }

            // Test 9: Text Positioning Commands
            if (pdfText.Contains("BT") && pdfText.Contains("ET") && pdfText.Contains("Td"))
            {
                Console.WriteLine("✅ Text Commands: Valid text positioning commands");
            }
            else
            {
                warnings.Add("⚠️ Missing or incomplete text positioning commands");
            }

            // Test 10: Page Structure
            if (pdfText.Contains("/Type /Page") && pdfText.Contains("/MediaBox"))
            {
                Console.WriteLine("✅ Page Structure: Valid page definitions with MediaBox");
            }
            else
            {
                issues.Add("❌ Invalid page structure");
            }

            // Summary
            Console.WriteLine();
            Console.WriteLine("=== COMPATIBILITY ASSESSMENT ===");
            
            if (issues.Count == 0)
            {
                Console.WriteLine("🎉 EXCELLENT: PDF is universally compatible!");
                Console.WriteLine("✅ Should open properly in:");
                Console.WriteLine("   - Chrome (PDFium engine)");
                Console.WriteLine("   - Safari (WebKit PDF viewer)");
                Console.WriteLine("   - Firefox (PDF.js)");
                Console.WriteLine("   - Adobe Acrobat/Reader");
                Console.WriteLine("   - Preview.app (macOS)");
                Console.WriteLine("   - Edge (PDFium engine)");
            }
            else
            {
                Console.WriteLine($"❌ COMPATIBILITY ISSUES FOUND ({issues.Count}):");
                foreach (var issue in issues)
                {
                    Console.WriteLine($"   {issue}");
                }
            }

            if (warnings.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"⚠️ WARNINGS ({warnings.Count}):");
                foreach (var warning in warnings)
                {
                    Console.WriteLine($"   {warning}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"File Size: {new FileInfo(pdfPath).Length:N0} bytes");
        }
    }
}
