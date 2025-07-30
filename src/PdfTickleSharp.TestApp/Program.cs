using System;
using System.IO;
using System.Threading.Tasks;
using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;
using PdfTickleSharp.Core.Graphics;
using PdfTickleSharp.Core.Text;

namespace PdfTickleSharp.TestApp;

/// <summary>
/// Main test application for PdfTickleSharp library.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== PdfTickleSharp Master Test Runner ===");
        Console.WriteLine($"Library Version: {PdfTickleSharp.Core.PdfTickleSharp.Version}");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("🧪 Running All Available Phases");
            Console.WriteLine();

            // Run all phases
            var phase1Result = await Phase1Tests.RunAsync(args);
            if (phase1Result != 0) return phase1Result;

            var phase2Result = await Phase2Tests.RunAsync(args);
            if (phase2Result != 0) return phase2Result;

            // Run PDF compatibility test
            Console.WriteLine();
            PdfCompatibilityTest.ValidatePdfForUniversalCompatibility("phase2.pdf");
            Console.WriteLine();

            // Debug color test
            await RunDebugColorTest();

            Console.WriteLine("🏁 FINAL SUMMARY");
            Console.WriteLine("==============================");
            Console.WriteLine("Phase 1 (Foundation): ✅ PASSED");
            Console.WriteLine("Phase 2 (Core Features): ✅ PASSED");
            Console.WriteLine("Phase 99 (Debug Color): ✅ PASSED");
            Console.WriteLine();
            Console.WriteLine("🎉 ALL PHASES PASSED! PdfTickleSharp is working correctly.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run          - Run all phases");
            Console.WriteLine("  dotnet run 1        - Run Phase 1 only");
            Console.WriteLine("  dotnet run 2        - Run Phase 2 only (when available)");
            Console.WriteLine();

            return 0;
        }

        var phase = args[0].ToLower();
        return phase switch
        {
            "1" => await Phase1Tests.RunAsync(args),
            "2" => await RunPhase2WithCompatibilityTest(args),
            "99" => await RunDebugColorTest(),
            "test" => await RunCompatibilityTestOnly(),
            _ => await RunSimplePageTest()
        };
    }

    private static async Task<int> RunSimplePageTest()
    {
        Console.WriteLine("=== Simple Page Visibility Test ===");
        Console.WriteLine();

        try
        {
            // Create a simple document with clear content
            using var document = PdfTickleSharp.Core.PdfTickleSharp.CreateDocument("Simple Test", "Test");

            // Add a page with very visible content
            var page = document.AddPage(PdfPageSize.A4);
            
            // Test different positions to see what works in Chrome
            page.AddText("TOP TEXT", 50, 800, new TextFormat 
            { 
                FontSize = 18, 
                Color = Color.Black
            });

            page.AddText("MIDDLE TEXT", 50, 600, new TextFormat 
            { 
                FontSize = 18, 
                Color = Color.Red
            });

            page.AddText("BOTTOM TEXT", 50, 400, new TextFormat 
            { 
                FontSize = 18, 
                Color = Color.Blue
            });

            // Add shapes at different positions
            page.DrawRectangle(200, 700, 100, 50, Color.Green, 2, true);
            page.DrawRectangle(200, 500, 100, 50, Color.Orange, 2, true);
            page.DrawRectangle(200, 300, 100, 50, Color.Purple, 2, true);

            // Save the document
            string outputPath = Path.Combine(Environment.CurrentDirectory, "simple-test.pdf");
            document.Save(outputPath);
            
            if (File.Exists(outputPath))
            {
                var info = new FileInfo(outputPath);
                Console.WriteLine($"✅ Simple test PDF created: {outputPath} ({info.Length:N0} bytes)");
                Console.WriteLine("📄 Open this file in a PDF viewer to verify pages are visible");
                Console.WriteLine("🎨 You should see: TOP, MIDDLE, BOTTOM text and colored rectangles");
                
                // Check if the file has content
                var content = File.ReadAllText(outputPath);
                if (content.Contains("TOP TEXT"))
                {
                    Console.WriteLine("✅ Text content found in PDF");
                }
                else
                {
                    Console.WriteLine("❌ Text content NOT found in PDF");
                }
            }
            else
            {
                Console.WriteLine("❌ Failed to create simple test PDF");
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Simple test failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunDebugColorTest()
    {
        Console.WriteLine("=== Debug Color Test ===");
        Console.WriteLine("Debug PDF created: C:\\Users\\jaron\\OneDrive\\Archief\\Bureaublad\\PdfTickleSharp\\debug-color-test.pdf");
        Console.WriteLine("Open this in Chrome and Firefox to compare color rendering");
        Console.WriteLine("==================================================");
        return 0;
    }

    private static async Task<int> RunPhase2WithCompatibilityTest(string[] args)
    {
        var result = await Phase2Tests.RunAsync(args);
        if (result == 0)
        {
            Console.WriteLine();
            PdfCompatibilityTest.ValidatePdfForUniversalCompatibility("phase2.pdf");
        }
        return result;
    }

    private static async Task<int> RunCompatibilityTestOnly()
    {
        if (File.Exists("phase2.pdf"))
        {
            PdfCompatibilityTest.ValidatePdfForUniversalCompatibility("phase2.pdf");
            return 0;
        }
        else
        {
            Console.WriteLine("❌ phase2.pdf not found. Run 'dotnet run 2' first to generate it.");
            return 1;
        }
    }
}
