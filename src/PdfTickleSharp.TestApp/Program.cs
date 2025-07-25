using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;

namespace PdfTickleSharp.TestApp;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== PdfTickleSharp Master Test Runner ===");
        Console.WriteLine($"Library Version: {PdfTickleSharp.Core.PdfTickleSharp.Version}");
        Console.WriteLine();

        var phases = new Dictionary<int, (string Name, string Description, Func<Task<bool>> Runner)>
        {
            { 1, ("Foundation", "Project structure, document model, basic text, file I/O", RunPhase1) }
            // Phase 2 will be added here when ready
            // { 2, ("Core Features", "Advanced text formatting, images, layout", RunPhase2) }
            // Phase 3 will be added here when ready  
            // { 3, ("Advanced Features", "Annotations, signatures, forms", RunPhase3) }
        };

        // Check for specific phase argument
        var targetPhase = 0;
        if (args.Length > 0 && int.TryParse(args[0], out var phaseArg) && phases.ContainsKey(phaseArg))
        {
            targetPhase = phaseArg;
        }

        try
        {
            if (targetPhase > 0)
            {
                // Run specific phase
                Console.WriteLine($"🎯 Running Phase {targetPhase} Only");
                Console.WriteLine($"📋 {phases[targetPhase].Name}: {phases[targetPhase].Description}");
                Console.WriteLine();
                
                var success = await phases[targetPhase].Runner();
                if (success)
                {
                    Console.WriteLine($"✅ Phase {targetPhase} completed successfully!");
                }
                else
                {
                    Console.WriteLine($"❌ Phase {targetPhase} failed!");
                    return 1;
                }
            }
            else
            {
                // Run all available phases
                Console.WriteLine("🧪 Running All Available Phases");
                Console.WriteLine();
                
                var allPassed = true;
                foreach (var (phaseNum, (name, description, runner)) in phases.OrderBy(x => x.Key))
                {
                    Console.WriteLine($"🚀 Starting Phase {phaseNum}: {name}");
                    Console.WriteLine($"📝 {description}");
                    Console.WriteLine(new string('=', 50));
                    
                    var success = await runner();
                    
                    Console.WriteLine(new string('=', 50));
                    if (success)
                    {
                        Console.WriteLine($"✅ Phase {phaseNum} PASSED");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Phase {phaseNum} FAILED");
                        allPassed = false;
                    }
                    Console.WriteLine();
                }
                
                // Final summary
                Console.WriteLine("🏁 FINAL SUMMARY");
                Console.WriteLine(new string('=', 30));
                foreach (var (phaseNum, (name, _, _)) in phases.OrderBy(x => x.Key))
                {
                    Console.WriteLine($"Phase {phaseNum} ({name}): ✅ PASSED");
                }
                
                if (allPassed)
                {
                    Console.WriteLine();
                    Console.WriteLine("🎉 ALL PHASES PASSED! PdfTickleSharp is working correctly.");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("❌ Some phases failed. Check output above for details.");
                    return 1;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 CRITICAL ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run          - Run all phases");
        Console.WriteLine("  dotnet run 1        - Run Phase 1 only");
        Console.WriteLine("  dotnet run 2        - Run Phase 2 only (when available)");
        
        return 0;
    }

    static async Task<bool> RunPhase1()
    {
        var result = await Phase1Tests.RunAsync(Array.Empty<string>());
        return result == 0;
    }

    // Placeholder for future phases
    // static async Task<bool> RunPhase2()
    // {
    //     Console.WriteLine("=== Phase 2: Core Features Test ===");
    //     // TODO: Implement Phase 2 tests
    //     await Task.Delay(100); // Placeholder
    //     return true;
    // }
} 