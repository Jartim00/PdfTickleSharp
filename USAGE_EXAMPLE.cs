using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;

/// <summary>
/// Example demonstrating the current PdfTickleSharp core document model API.
/// NOTE: PDF generation (Save methods) are not yet implemented.
/// </summary>
public class UsageExample
{
    public static void DemonstrateCoreAPI()
    {
        // Create a new PDF document
        using var document = PdfTickleSharp.CreateDocument("My First PDF", "John Doe");
        
        // Add pages with different sizes
        var page1 = document.AddPage(PdfPageSize.A4);
        var page2 = document.AddPage(PdfPageSize.Letter);
        
        // Add content to pages
        page1.AddText("Welcome to PdfTickleSharp!", 100, 700);
        page1.AddText("This is an A4 page", 100, 650);
        
        page2.AddText("This is a Letter-sized page", 100, 700);
        page2.AddText("Different page sizes are supported", 100, 650);
        
        // Display document information
        Console.WriteLine($"Created: {document}");
        Console.WriteLine($"Author: {document.Metadata.Author}");
        Console.WriteLine($"Title: {document.Metadata.Title}");
        Console.WriteLine($"Pages: {document.PageCount}");
        
        foreach (var page in document.Pages)
        {
            Console.WriteLine($"  {page}");
        }
        
        // Standard page sizes available
        Console.WriteLine("\nStandard page sizes:");
        Console.WriteLine($"A4: {PdfPageSize.A4}");
        Console.WriteLine($"Letter: {PdfPageSize.Letter}");
        Console.WriteLine($"A3: {PdfPageSize.A3}");
        Console.WriteLine($"Legal: {PdfPageSize.Legal}");
        
        // Custom page size
        var customSize = PdfPageSize.Custom(400, 600);
        var customPage = document.AddPage(customSize);
        customPage.AddText("Custom sized page", 50, 550);
        
        Console.WriteLine($"Custom page: {customPage}");
        
        // Note: Actual PDF generation will be available in the next phase
        Console.WriteLine("\nNote: PDF file generation is not yet implemented.");
        Console.WriteLine("This demonstrates the document model API structure.");
        Console.WriteLine("Next phase: Implement actual PDF file I/O and rendering.");
    }
} 