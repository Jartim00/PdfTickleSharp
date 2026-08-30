using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;

namespace PdfTickleSharp.TestApp;

public static class MinimalTest
{
    public static Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("=== Minimal PDF.js Compatibility Test ===");
        
        // Create a minimal PDF with just one page
        using var document = PdfTickleSharp.Core.PdfTickleSharp.CreateDocument("Minimal Test", "PdfTickleSharp");
        var page = document.AddPage(PdfPageSize.A4);
        page.AddText("Test", 100, 750);
        
        // Save to file
        document.Save("minimal-test.pdf");
        
        Console.WriteLine("✅ Minimal PDF created: minimal-test.pdf");
        return Task.FromResult(0);
    }
} 