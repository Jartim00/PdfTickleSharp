using System;
using System.Collections.Generic;
using System.Linq;
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
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== PdfTickleSharp Master Test Runner ===");
        Console.WriteLine($"Library Version: {PdfTickleSharp.Core.PdfTickleSharp.Version}");
        Console.WriteLine();

        if (args.Length == 0) return await RunAllPhases(args);

        return args[0].ToLowerInvariant() switch
        {
            "1" => await Phase1Tests.RunAsync(args),
            "2" => await RunPhase2WithCompatibilityTest(args),
            "99" => RunDebugColorTest(),
            "test" => RunCompatibilityTestOnly(),
            "minimal" => await MinimalTest.RunAsync(args),
            "simple" => RunSimplePageTest(),
            _ => ShowUsage()
        };
    }

    /// <summary>
    /// Runs every phase and reports the real outcome of each one.
    /// </summary>
    private static async Task<TestRunOutcome> RunAllPhasesCore(string[] args)
    {
        var results = new Dictionary<string, bool>();

        results["Phase 1 (Foundation)"] = await Phase1Tests.RunAsync(args) == 0;
        results["Phase 2 (Core Features)"] = await Phase2Tests.RunAsync(args) == 0;

        Console.WriteLine();
        results["PDF Compatibility"] = File.Exists("phase2.pdf")
                                       && PdfCompatibilityTest.ValidatePdfForUniversalCompatibility("phase2.pdf");

        Console.WriteLine();
        results["Colour Rendering"] = RunDebugColorTest() == 0;

        return new TestRunOutcome(results);
    }

    private static async Task<int> RunAllPhases(string[] args)
    {
        Console.WriteLine("🧪 Running All Available Phases");
        Console.WriteLine();

        var outcome = await RunAllPhasesCore(args);

        Console.WriteLine();
        Console.WriteLine("🏁 FINAL SUMMARY");
        Console.WriteLine("==============================");

        foreach (var (name, passed) in outcome.Results)
        {
            Console.WriteLine($"{name}: {(passed ? "✅ PASSED" : "❌ FAILED")}");
        }

        Console.WriteLine();
        if (outcome.AllPassed)
        {
            Console.WriteLine("🎉 ALL PHASES PASSED! PdfTickleSharp is working correctly.");
        }
        else
        {
            Console.WriteLine("❌ SOME CHECKS FAILED. See the details above.");
        }

        Console.WriteLine();
        ShowUsage();

        return outcome.AllPassed ? 0 : 1;
    }

    private static int ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run          - Run all phases and validate the output");
        Console.WriteLine("  dotnet run 1        - Run Phase 1 only (foundation)");
        Console.WriteLine("  dotnet run 2        - Run Phase 2 only (core features)");
        Console.WriteLine("  dotnet run 99       - Colour rendering check");
        Console.WriteLine("  dotnet run test     - Validate an existing phase2.pdf");
        Console.WriteLine("  dotnet run minimal  - Create the smallest possible PDF");
        Console.WriteLine("  dotnet run simple   - Create a page-visibility test PDF");
        return 0;
    }

    /// <summary>
    /// Creates a page with content at known positions, then verifies the result is
    /// a structurally valid PDF. Positions run bottom-up, matching PDF's own
    /// coordinate system, so "TOP" has the largest Y.
    /// </summary>
    private static int RunSimplePageTest()
    {
        Console.WriteLine("=== Simple Page Visibility Test ===");
        Console.WriteLine();

        try
        {
            using var document = PdfTickleSharp.Core.PdfTickleSharp.CreateDocument("Simple Test", "Test");
            var page = document.AddPage(PdfPageSize.A4);

            page.AddText("TOP TEXT", 50, 800, new TextFormat { FontSize = 18, Color = Color.Black });
            page.AddText("MIDDLE TEXT", 50, 600, new TextFormat { FontSize = 18, Color = Color.Red });
            page.AddText("BOTTOM TEXT", 50, 400, new TextFormat { FontSize = 18, Color = Color.Blue });

            page.DrawRectangle(200, 700, 100, 50, Color.Green, 2, true);
            page.DrawRectangle(200, 500, 100, 50, Color.Orange, 2, true);
            page.DrawRectangle(200, 300, 100, 50, Color.Purple, 2, true);

            var outputPath = Path.Combine(Environment.CurrentDirectory, "simple-test.pdf");
            document.Save(outputPath);

            if (!File.Exists(outputPath))
            {
                Console.WriteLine("❌ Failed to create simple test PDF");
                return 1;
            }

            var info = new FileInfo(outputPath);
            Console.WriteLine($"✅ Simple test PDF created: {outputPath} ({info.Length:N0} bytes)");
            Console.WriteLine("🎨 You should see: TOP, MIDDLE, BOTTOM text and colored rectangles");
            Console.WriteLine();

            // Text is stored as glyph indices in an embedded font, so it cannot be
            // found by searching the raw bytes; validate the structure instead.
            return PdfCompatibilityTest.ValidatePdfForUniversalCompatibility(outputPath) ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Simple test failed: {ex.Message}");
            return 1;
        }
    }

    private static int RunDebugColorTest()
    {
        try
        {
            var outputPath = DebugColorTest.Run();
            return PdfCompatibilityTest.ValidatePdfForUniversalCompatibility(outputPath) ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Colour rendering check failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunPhase2WithCompatibilityTest(string[] args)
    {
        var result = await Phase2Tests.RunAsync(args);
        if (result != 0) return result;

        Console.WriteLine();
        return PdfCompatibilityTest.ValidatePdfForUniversalCompatibility("phase2.pdf") ? 0 : 1;
    }

    private static int RunCompatibilityTestOnly()
    {
        if (!File.Exists("phase2.pdf"))
        {
            Console.WriteLine("❌ phase2.pdf not found. Run 'dotnet run 2' first to generate it.");
            return 1;
        }
        return PdfCompatibilityTest.ValidatePdfForUniversalCompatibility("phase2.pdf") ? 0 : 1;
    }

    /// <summary>
    /// The outcome of a full test run, keeping each check's result.
    /// </summary>
    private sealed class TestRunOutcome
    {
        public Dictionary<string, bool> Results { get; }
        public bool AllPassed => Results.Values.All(passed => passed);

        public TestRunOutcome(Dictionary<string, bool> results) => Results = results;
    }
}
