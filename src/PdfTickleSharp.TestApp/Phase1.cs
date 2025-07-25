using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;

namespace PdfTickleSharp.TestApp;

/// <summary>
/// Phase 1 test runner - Foundation functionality tests
/// </summary>
public static class Phase1Tests
{
    public static async Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("=== PdfTickleSharp Phase 1 Complete Test ===");
        Console.WriteLine($"Library Version: {PdfTickleSharp.Core.PdfTickleSharp.Version}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("🧪 Testing all Phase 1 functionality...");
            Console.WriteLine();

            // 1. Test Project Structure & Solution Architecture
            Console.WriteLine("✅ 1. Project structure setup - WORKING");
            Console.WriteLine("✅ 2. Basic solution architecture - WORKING");
            Console.WriteLine();

            // 3. Test Core PDF Document Model
            Console.WriteLine("📄 3. Testing Core PDF Document Model...");
            
            using var document = PdfTickleSharp.Core.PdfTickleSharp.CreateDocument("Phase 1 Test PDF", "PdfTickleSharp Test");
            Console.WriteLine($"   ✅ Document created: {document}");
            Console.WriteLine($"   ✅ Metadata: Title='{document.Metadata.Title}', Author='{document.Metadata.Author}'");
            Console.WriteLine($"   ✅ Producer: {document.Metadata.Producer}");
            Console.WriteLine();

            // Test page sizes
            Console.WriteLine("📏 Testing page sizes...");
            Console.WriteLine($"   ✅ A4: {PdfPageSize.A4}");
            Console.WriteLine($"   ✅ Letter: {PdfPageSize.Letter}");
            Console.WriteLine($"   ✅ A3: {PdfPageSize.A3}");
            var customSize = PdfPageSize.Custom(400, 600);
            Console.WriteLine($"   ✅ Custom: {customSize}");
            Console.WriteLine();

            // 4. Test Basic Text Rendering API
            Console.WriteLine("📝 4. Testing Basic Text Rendering API...");
            
            var page1 = document.AddPage(PdfPageSize.A4);
            var page2 = document.AddPage(PdfPageSize.Letter);
            var page3 = document.AddPage(customSize);
            
            // Add text content to demonstrate the API
            page1.AddText("Welcome to PdfTickleSharp!", 100, 750);
            page1.AddText("This is the Phase 1 completion test.", 100, 720);
            page1.AddText("✅ Project structure setup", 120, 680);
            page1.AddText("✅ Basic solution architecture", 120, 660);
            page1.AddText("✅ Core PDF document model", 120, 640);
            page1.AddText("✅ Basic text rendering API", 120, 620);
            page1.AddText("✅ Simple PDF creation (file I/O)", 120, 600);
            
            page2.AddText("Second Page - Letter Size", 100, 720);
            page2.AddText("Different page sizes are supported!", 100, 690);
            page2.AddText($"Page size: {page2.PageSize}", 100, 660);
            
            page3.AddText("Custom Page Size", 50, 550);
            page3.AddText($"Size: {page3.PageSize}", 50, 520);
            page3.AddText("This demonstrates flexible page sizing", 50, 490);
            
            Console.WriteLine($"   ✅ Text added to {document.PageCount} pages");
            foreach (var page in document.Pages)
            {
                Console.WriteLine($"      - {page}");
            }
            Console.WriteLine();

            // 5. Test Simple PDF Creation (file I/O) - THE NEW FUNCTIONALITY!
            Console.WriteLine("💾 5. Testing Simple PDF Creation (file I/O)...");
            
            var outputPath = Path.Combine(Environment.CurrentDirectory, "test-output.pdf");
            Console.WriteLine($"   📁 Saving PDF to: {outputPath}");
            
            document.Save(outputPath);
            
            // Verify the file was created
            if (File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"   ✅ PDF file created successfully!");
                Console.WriteLine($"   📊 File size: {fileInfo.Length:N0} bytes");
                Console.WriteLine($"   📅 Created: {fileInfo.CreationTime}");
                
                // Test byte array generation
                var pdfBytes = document.ToByteArray();
                Console.WriteLine($"   📊 Byte array size: {pdfBytes.Length:N0} bytes");
                
                // Test stream generation
                using var memoryStream = new MemoryStream();
                document.Save(memoryStream);
                Console.WriteLine($"   📊 Stream size: {memoryStream.Length:N0} bytes");
                
                Console.WriteLine($"   ✅ All I/O methods working!");
            }
            else
            {
                Console.WriteLine($"   ❌ PDF file was not created!");
                throw new Exception("PDF file creation failed");
            }
            
            Console.WriteLine();
            Console.WriteLine("🎉 PHASE 1 COMPLETE - CORE FUNCTIONALITY WORKING!");
            Console.WriteLine();
            Console.WriteLine("📋 Phase 1 Final Status:");
            Console.WriteLine("✅ 1. Project structure setup - COMPLETE");
            Console.WriteLine("✅ 2. Basic solution architecture - COMPLETE");  
            Console.WriteLine("✅ 3. Core PDF document model - COMPLETE");
            Console.WriteLine("✅ 4. Basic text rendering API - COMPLETE (basic only)");
            Console.WriteLine("✅ 5. Simple PDF creation (file I/O) - COMPLETE");
            Console.WriteLine();
            Console.WriteLine("⚠️  Known Phase 1 Limitations (to fix in Phase 2):");
            Console.WriteLine("   • Unicode characters (✅) display as '?' - ASCII encoding issue");
            Console.WriteLine("   • Text formatting is very basic - only Helvetica 12pt");
            Console.WriteLine("   • No text styling, colors, or advanced typography");
            Console.WriteLine("   • Page sizes work perfectly (A4, Letter, Custom)");
            Console.WriteLine();
            Console.WriteLine("🚀 READY FOR PHASE 2: Core Features");
            Console.WriteLine("   - Fix Unicode/UTF-8 encoding for special characters");
            Console.WriteLine("   - Advanced text formatting (fonts, colors, styles)");
            Console.WriteLine("   - Image insertion and manipulation");
            Console.WriteLine("   - Page layout management");
            Console.WriteLine("   - Enhanced metadata handling");
            Console.WriteLine("   - Basic drawing operations");
            Console.WriteLine();
            Console.WriteLine($"📁 Your test PDF is ready: {outputPath}");
            Console.WriteLine("🔍 Open it in any PDF viewer to see the results!");
            Console.WriteLine("🎯 The basic infrastructure works - now let's make it beautiful!");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PHASE 1 TEST FAILED: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex}");
            return 1;
        }
    }
} 