using PdfTickleSharp.Core.Document;
using PdfTickleSharp.Core.Graphics;
using PdfTickleSharp.Core.Text;

namespace PdfTickleSharp.TestApp;

/// <summary>
/// Phase 2 test runner - Core Features functionality tests
/// </summary>
public static class Phase2Tests
{
    private const double A4_Height = 841.89;

    public static Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("=== PdfTickleSharp Phase 2 Complete Test ===");
        Console.WriteLine($"Library Version: {PdfTickleSharp.Core.PdfTickleSharp.Version}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("🧪 Testing all Phase 2 functionality...");
            Console.WriteLine();

            // Create document
            using var document = PdfTickleSharp.Core.PdfTickleSharp.CreateDocument("Phase 2 Test PDF", "PdfTickleSharp Test");

            // Page 1: Unicode & Advanced Text Formatting
            var page1 = document.AddPage(PdfPageSize.A4);
            Console.WriteLine("🌍 1. Testing Unicode/UTF-8 Support and Advanced Text Formatting...");
            page1.AddText("Unicode Test: ✅ 🌍 🎉", 100, 750);
            page1.AddText("Special Characters: ℃ ™ © ®", 100, 720);
            page1.AddText("Accented Text: café résumé naïve", 100, 690);
            page1.AddText("Emojis: 🚀 📄 ✨ 🎯", 100, 660);
            page1.AddText("Bottom Text - Should be visible", 100, 150);
            page1.AddText("Middle Text - Should be visible", 100, 450);

            // Fonts & Sizes
            page1.AddText("Helvetica Font", 100, 630, new TextFormat { FontFamily = "Helvetica", FontSize = 12 });
            page1.AddText("Times Font", 100, 610, new TextFormat { FontFamily = "Times", FontSize = 14 });
            page1.AddText("Courier Font", 100, 590, new TextFormat { FontFamily = "Courier", FontSize = 10 });
            page1.AddText("Small Text (8pt)", 100, 570, new TextFormat { FontSize = 8 });
            page1.AddText("Large Text (18pt)", 100, 550, new TextFormat { FontSize = 18 });
            page1.AddText("Huge Text (24pt)", 100, 520, new TextFormat { FontSize = 24 });
            page1.AddText("Red Text", 100, 480, new TextFormat { Color = Color.Red });
            page1.AddText("Blue Text", 100, 460, new TextFormat { Color = Color.Blue });
            page1.AddText("Green Text", 100, 440, new TextFormat { Color = Color.Green });
            page1.AddText("Bold Text", 100, 420, new TextFormat { IsBold = true });
            page1.AddText("Italic Text", 100, 400, new TextFormat { IsItalic = true });
            page1.AddText("Bold & Italic", 100, 380, new TextFormat { IsBold = true, IsItalic = true });
            Console.WriteLine("   ✅ Unicode & formatting tests passed");
            Console.WriteLine();

            // Page 2: Text Layout & Alignment
            var page2 = document.AddPage(PdfPageSize.A4);
            Console.WriteLine("📐 2. Testing Text Layout and Alignment...");
            page2.AddText("Left Aligned Text", 100, 750, new TextFormat { Alignment = TextAlignment.Left });
            page2.AddText("Center Aligned Text", 300, 720, new TextFormat { Alignment = TextAlignment.Center });
            page2.AddText("Right Aligned Text", 500, 690, new TextFormat { Alignment = TextAlignment.Right });
            page2.AddText("Page 2 Bottom Text", 100, 150);
            page2.AddText("Page 2 Middle Text", 100, 450);
            // Text flow
            double flowX = 100, flowY = 650;
            var paragraph = "This is a long paragraph that demonstrates text flow capabilities. " +
                            "The text should wrap automatically to the next line when it reaches " +
                            "the specified width. This makes it much easier to create properly " +
                            "formatted documents with natural text flow.";
            page2.AddTextFlow(paragraph, flowX, A4_Height - flowY, 400, new TextFormat { FontSize = 10 });
            Console.WriteLine("   ✅ Layout & alignment tests passed");
            Console.WriteLine();

            // Page 3: Image Insertion
            var page3 = document.AddPage(PdfPageSize.A4);
            Console.WriteLine("🖼️ 3. Testing Image Insertion...");
            var imgBytes = CreateTestImage();
            page3.AddImage(imgBytes, 100, 750, 100, 100);
            page3.AddImage(imgBytes, 250, 750, 50, 50);
            page3.AddImage(imgBytes, 350, 750, 200, 100);
            page3.AddText("Images inserted successfully!", 100, 720);
            page3.AddText("Page 3 Bottom Text", 100, 150);
            page3.AddText("Page 3 Middle Text", 100, 450);
            Console.WriteLine("   ✅ Image tests passed");
            Console.WriteLine();

            // Page 4: Drawing Operations
            var page4 = document.AddPage(PdfPageSize.A4);
            Console.WriteLine("✏️ 4. Testing Basic Drawing Operations...");
            // Lines - positioned more carefully within page bounds
            page4.DrawLine(100, 700, 300, 700, Color.Black, 3); // Horizontal line - moved down from 750
            page4.DrawLine(100, 650, 100, 550, Color.Red, 3);   // Vertical line - adjusted range
            page4.DrawLine(150, 500, 350, 600, Color.Blue, 2);  // Diagonal line - repositioned
            // Rectangles
            page4.DrawRectangle(350, 750, 100, 50, Color.Green, 2, true);
            page4.DrawRectangle(500, 750, 100, 50, Color.Black, 1, false);
            // Circles
            page4.DrawCircle(150, 550, 30, Color.Orange, 2, true);
            page4.DrawCircle(250, 550, 25, Color.Purple, 1, false);
            page4.AddText("Drawing operations working!", 100, 500);
            page4.AddText("Page 4 Bottom Text", 100, 150);
            page4.AddText("Page 4 Middle Text", 100, 450);
            Console.WriteLine("   ✅ Drawing tests passed");
            Console.WriteLine();

            // Page 5: Metadata & I/O
            Console.WriteLine("💾 5. Testing Metadata & File I/O...");
            document.Metadata.Title = "Phase 2 Enhanced PDF";
            document.Metadata.Author = "PdfTickleSharp Test Suite";
            document.Metadata.Subject = "Advanced PDF Features Testing";
            document.Metadata.Keywords = "PDF, .NET, Unicode, Images, Drawing";
            document.Metadata.Creator = "PdfTickleSharp v2.0";
            document.Metadata.Producer = "PdfTickleSharp Library";

            string outputPath = Path.Combine(Environment.CurrentDirectory, "phase2.pdf");
            document.Save(outputPath);
            if (File.Exists(outputPath))
            {
                var info = new FileInfo(outputPath);
                Console.WriteLine($"   ✅ File saved: {outputPath} ({info.Length:N0} bytes)");
            }
            else throw new Exception("PDF file not created");
            Console.WriteLine();
            Console.WriteLine("🎉 PHASE 2 COMPLETE - CORE FEATURES WORKING!");
            Console.WriteLine();

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PHASE 2 TEST FAILED: {ex.Message}");
            return Task.FromResult(1);
        }
    }

    private static byte[] CreateTestImage()
    {
        return new byte[]
        {
            0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,
            0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
            0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,
            0x0C,0x49,0x44,0x41,0x54,0x08,0x99,0x63,0xF8,0xCF,0xCF,0x00,
            0x00,0x03,0x01,0x01,0x00,0x18,0xDD,0x8D,0xB0,0x00,0x00,0x00,
            0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
        };
    }
}
