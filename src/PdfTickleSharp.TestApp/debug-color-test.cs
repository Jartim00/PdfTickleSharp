using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;
using PdfTickleSharp.Core.Text;
using PdfTickleSharp.Core.Graphics;

namespace PdfTickleSharp.TestApp;

public static class DebugColorTest
{
    /// <summary>
    /// Writes a PDF exercising text colours at a range of page positions.
    /// </summary>
    /// <returns>The path of the generated PDF.</returns>
    public static string Run()
    {
        Console.WriteLine("=== Debug Color Test ===");
        
        using var document = PdfTickleSharp.Core.PdfTickleSharp.CreateDocument("Color Debug Test", "Debug");
        var page = document.AddPage(PdfPageSize.A4);
        
        // Test 1: Default black text
        page.AddText("Default Black Text", 100, 750);
        
        // Test 2: Red text
        page.AddText("Red Text", 100, 720, new TextFormat { Color = Color.Red });
        
        // Test 3: Blue text
        page.AddText("Blue Text", 100, 690, new TextFormat { Color = Color.Blue });
        
        // Test 4: Green text
        page.AddText("Green Text", 100, 660, new TextFormat { Color = Color.Green });
        
        // Test 5: Custom RGB color
        page.AddText("Custom Color", 100, 630, new TextFormat { Color = new Color(255, 128, 0) });
        
        // Add some text at lower positions to ensure visibility
        page.AddText("Bottom Text - Should be visible", 100, 100);
        page.AddText("Middle Text - Should be visible", 100, 400);
        
        var outputPath = Path.Combine(Environment.CurrentDirectory, "debug-color-test.pdf");
        document.Save(outputPath);
        
        Console.WriteLine($"Debug PDF created: {outputPath}");
        Console.WriteLine("Open this in Chrome and Firefox to compare color rendering");
        Console.WriteLine();

        return outputPath;
    }
} 