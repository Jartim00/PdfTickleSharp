using System;
using System.IO;
using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;
using PdfTickleSharp.Core.Graphics;
using PdfTickleSharp.Core.Text;

// ========================================
// PdfTickleSharp Usage Examples
// ========================================
// This file demonstrates the completed Phase 1 & 2 functionality.
//
// Coordinates: X and Y are in points and measured from the BOTTOM-LEFT corner
// of the page, matching PDF's own coordinate system. A larger Y is higher up
// the page, so on A4 (595 x 842 pt) y=750 is near the top and y=50 near the bottom.

public class UsageExamples
{
    public static void CreateBasicDocument()
    {
        // Phase 1: Basic PDF Creation
        using var document = PdfTickleSharp.CreateDocument("My First PDF", "PdfTickleSharp Demo");
        
        // Add pages with different sizes
        var page1 = document.AddPage(PdfPageSize.A4);
        var page2 = document.AddPage(PdfPageSize.Letter);
        
        // Add basic text (Phase 1)
        page1.AddText("Hello, PdfTickleSharp!", 100, 750);
        page1.AddText("This is a basic document.", 100, 720);
        
        // Save the document
        document.Save("basic-document.pdf");
    }
    
    public static void CreateAdvancedDocument()
    {
        // Phase 2: Advanced Features
        using var document = PdfTickleSharp.CreateDocument("Advanced PDF Demo", "PdfTickleSharp");
        
        var page = document.AddPage(PdfPageSize.A4);
        
        // Unicode and special characters (Phase 2).
        // Text may mix scripts and emoji freely: characters the chosen family does
        // not have fall back to another bundled font automatically. Emoji come from
        // Noto Emoji and draw as monochrome outlines, not in colour.
        page.AddText("Unicode Test: ✔ ← ⇒ 🌍 🚀 🎉", 100, 750);
        page.AddText("Special Characters: ℃ ™ © ®", 100, 720);
        page.AddText("Math Symbols: ± × ÷ √ ∞ ≤ ≥ ≠", 100, 690);
        page.AddText("Accented Text: café résumé naïve", 100, 660);
        
        // Advanced text formatting (Phase 2)
        page.AddText("Large Red Text", 100, 650, new TextFormat 
        { 
            FontSize = 18, 
            Color = Color.Red 
        });
        
        page.AddText("Bold Blue Text", 100, 620, new TextFormat 
        { 
            FontSize = 14, 
            Color = Color.Blue, 
            IsBold = true 
        });
        
        page.AddText("Italic Green Text", 100, 590, new TextFormat 
        { 
            FontSize = 12, 
            Color = Color.Green, 
            IsItalic = true 
        });
        
        // Text alignment (Phase 2)
        page.AddText("Left Aligned", 100, 560, new TextFormat { Alignment = TextAlignment.Left });
        page.AddText("Center Aligned", 300, 530, new TextFormat { Alignment = TextAlignment.Center });
        page.AddText("Right Aligned", 500, 500, new TextFormat { Alignment = TextAlignment.Right });
        
        // Text flow with wrapping (Phase 2)
        var paragraph = "This is a long paragraph that demonstrates text flow capabilities. " +
                       "The text should wrap automatically to the next line when it reaches " +
                       "the specified width. This makes it much easier to create properly " +
                       "formatted documents with natural text flow.";
        page.AddTextFlow(paragraph, 100, 450, 400, new TextFormat { FontSize = 10 });
        
        // Drawing operations (Phase 2).
        // Rectangles are positioned by their bottom-left corner; circles by centre.
        page.DrawLine(100, 400, 300, 400, Color.Black, 2);
        page.DrawRectangle(350, 400, 100, 50, Color.Green, 2, true);
        page.DrawCircle(500, 425, 25, Color.Orange, 2, true);

        // Image insertion (Phase 2). PNG and JPEG are supported, including PNG
        // transparency. X and Y are the image's bottom-left corner, and width and
        // height are the size on the page in points, independent of pixel size.
        var imageBytes = File.ReadAllBytes("logo.png");
        page.AddImage(imageBytes, 100, 250, 120, 80);

        // Save the document
        document.Save("advanced-document.pdf");
    }

    public static void MeasureTextBeforeDrawing()
    {
        // Text is measured with the real metrics of the font that will draw it,
        // which is useful for laying out boxes, tables or columns yourself.
        var format = new TextFormat { FontSize = 12, IsBold = true };
        var width = FontManager.MeasureText("How wide is this?", format);

        // Check up front whether every character can actually be rendered.
        var renderable = FontManager.CanRender("Unicode ✔ but not 🌍");

        Console.WriteLine($"Width: {width:F1} pt, fully renderable: {renderable}");
    }
    
    public static void CreateMultiPageDocument()
    {
        using var document = PdfTickleSharp.CreateDocument("Multi-Page Demo", "PdfTickleSharp");
        
        // Page 1: A4 with content
        var page1 = document.AddPage(PdfPageSize.A4);
        page1.AddText("Page 1 - A4 Size", 100, 750, new TextFormat { FontSize = 16, IsBold = true });
        page1.AddText("This is the first page with A4 dimensions.", 100, 720);
        page1.DrawRectangle(100, 650, 200, 100, Color.Blue, 3, false);
        
        // Page 2: Letter size
        var page2 = document.AddPage(PdfPageSize.Letter);
        page2.AddText("Page 2 - Letter Size", 100, 720, new TextFormat { FontSize = 16, IsBold = true });
        page2.AddText("This is the second page with Letter dimensions.", 100, 690);
        page2.DrawCircle(150, 600, 50, Color.Red, 2, true);
        
        // Page 3: Custom size
        var page3 = document.AddPage(PdfPageSize.Custom(400, 600));
        page3.AddText("Page 3 - Custom Size", 50, 550, new TextFormat { FontSize = 16, IsBold = true });
        page3.AddText("This page has custom dimensions.", 50, 520);
        page3.DrawLine(50, 450, 350, 450, Color.Green, 4);
        
        document.Save("multi-page-document.pdf");
    }
}

// ========================================
// Quick Start Guide
// ========================================

/*
To run these examples:

1. Build the solution:
   dotnet build src/PdfTickleSharp.sln

2. Run the test suite to verify everything works:
   cd src/PdfTickleSharp.TestApp
   dotnet run

3. Create your own PDFs using the examples above!

Phase 1 Features ✅:
- Basic PDF creation
- Multiple page sizes (A4, Letter, A3, Custom)
- Simple text positioning
- File I/O (save to file, stream, byte array)

Phase 2 Features ✅:
- Unicode support via subsetted, embedded TrueType fonts (text stays searchable)
- Advanced text formatting (font families, sizes, colors, bold, italic)
- Text alignment (left, center, right) using real font metrics
- Text flow with automatic wrapping and font-derived line spacing
- Drawing operations (lines, rectangles, circles)
- Image insertion (PNG and JPEG, including PNG alpha transparency)
- Document metadata written to the PDF /Info dictionary

Known limitations:
- Emoji (🌍 🚀 🎉) render as monochrome outlines from Noto Emoji, not in colour.
  Colour emoji would mean translating a COLRv1 paint graph — gradients, transforms
  and compositing — into PDF shading patterns, which is left to a later phase.
- Font families map onto the bundled DejaVu faces: Helvetica/Arial to DejaVuSans,
  Times to DejaVuSerif, Courier to DejaVuSansMono. Metrics are exact for the font
  used, but shapes are not identical to the fonts they stand in for.
- Text is drawn as single lines or wrapped flows; there is no automatic page
  overflow, so content past the bottom of a page is not moved to the next one.
*/ 