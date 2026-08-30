# PdfTickleSharp

A modern, .NET Core-first PDF library that provides comprehensive PDF manipulation capabilities without the high licensing costs of commercial alternatives.

## 🎯 Project Vision

PdfTickleSharp aims to be a complete, open-source alternative to expensive commercial PDF libraries like Aspose.PDF for .NET. Our goal is to provide all essential PDF operations with a clean, intuitive API that developers love to use.

## ✨ Features

### Core Features (PdfTickleSharp.Core) - Phase 1 Complete ✅
- ✅ **PDF Creation**: Generate PDFs from scratch with basic text positioning
- ✅ **Document Model**: Complete PDF document structure with pages and metadata
- ✅ **Page Management**: Multiple page sizes (A4, Letter, A3, Custom) and multi-page support
- ✅ **Basic Text Rendering**: Simple text placement with coordinate positioning
- ✅ **File I/O Operations**: Save to file, stream, or byte array
- ⚠️ **Known Limitations**: ASCII encoding only (Unicode shows as '?'), basic Helvetica 12pt font only

### Core Features (PdfTickleSharp.Core) - Phase 2 Complete ✅
- ✅ **Unicode Support**: Text is drawn with subsetted, embedded TrueType fonts and stays selectable, copyable and searchable
- ✅ **Advanced Text Formatting**: Font families, sizes, colors, bold and italic from real font files
- ✅ **Enhanced Layout**: Alignment and word wrapping measured with the font's own metrics, line spacing derived from the typeface
- ✅ **Image Insertion**: PNG and JPEG, including PNG alpha transparency via soft masks
- ✅ **Basic Drawing**: Lines, rectangles and circles, filled or outlined
- ✅ **Document Metadata**: Written to the PDF `/Info` dictionary
- ✅ **Emoji**: Rendered as monochrome outlines from Noto Emoji — see [Known Limitations](#-known-limitations) for why they are not in colour

### Advanced Features (PdfTickleSharp.Advanced) - Phase 3+ 🚧
- 🚧 **Annotations**: Add, modify, and remove PDF annotations
- 🚧 **Digital Signatures**: Sign and verify PDF documents
- 🚧 **PDF/A Conversion**: Convert documents to PDF/A standards for archival
- 🚧 **Form Handling**: Create and fill interactive PDF forms
- 🚧 **Encryption**: Password protection and security features
- 🚧 **OCR Integration**: Text recognition in scanned documents

*Legend: ✅ Available, ⚠️ Basic/Limited, 🚧 Planned*

## 🏗️ Project Structure

```
PdfTickleSharp/
├── src/
│   ├── PdfTickleSharp.sln                    # Main solution file
│   ├── PdfTickleSharp.Core/                  # Core PDF functionality ✅
│   │   ├── PdfTickleSharp.Core.csproj
│   │   ├── Document/                         # PDF document handling ✅
│   │   │   ├── PdfDocument.cs               # Main document class
│   │   │   ├── PdfPage.cs                   # Page management
│   │   │   ├── PdfPageSize.cs               # Page size definitions
│   │   │   ├── PdfMetadata.cs               # Document metadata
│   │   │   └── PageContent.cs               # Text, image and shape elements
│   │   ├── IO/                              # File I/O operations ✅
│   │   │   └── PdfWriter.cs                 # PDF file generation
│   │   ├── Graphics/                        # Colour and images ✅
│   │   │   ├── Color.cs                     # RGB colour model
│   │   │   └── ImageDecoder.cs              # PNG/JPEG decoding for embedding
│   │   ├── Text/                            # Text and font handling ✅
│   │   │   ├── TextFormat.cs                # Font family, size, colour, style
│   │   │   ├── TextAlignment.cs             # Alignment options
│   │   │   ├── FontManager.cs               # Font resolution, measuring, fallback
│   │   │   └── TrueTypeFont.cs              # TrueType parsing and subsetting
│   │   └── PdfTickleSharp.cs                # Main library entry point
│   ├── PdfTickleSharp.Data/                 # Embedded resources ✅
│   │   ├── ResourceManager.cs               # Font/resource loading, PNG encoding
│   │   └── Fonts/                           # Bundled DejaVu font files
│   ├── PdfTickleSharp.Advanced/              # Advanced features (future)
│   │   └── PdfTickleSharp.Advanced.csproj
│   └── PdfTickleSharp.TestApp/              # Comprehensive test suite ✅
│       ├── PdfTickleSharp.TestApp.csproj
│       ├── Program.cs                       # Master test runner
│       ├── Phase1.cs                        # Phase 1 specific tests
│       ├── Phase2.cs                        # Phase 2 specific tests
│       └── PDF_Compatibility_Test.cs        # Structural PDF validator
├── USAGE_EXAMPLE.cs                          # Usage examples
├── .gitignore                               # Git ignore rules
└── README.md                                # This file
```

## 🚀 Quick Start

### Prerequisites

- .NET 6.0 or later
- Visual Studio 2022 or JetBrains Rider (recommended)
- Git

### Building the Solution

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/PdfTickleSharp.git
   cd PdfTickleSharp
   ```

2. **Restore dependencies**
   ```bash
   cd src
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run Phase 1 tests**
   ```bash
   cd PdfTickleSharp.TestApp
   dotnet run          # Run all phases
   dotnet run 1        # Run Phase 1 only
   ```

### Basic Usage Example

```csharp
using PdfTickleSharp.Core;
using PdfTickleSharp.Core.Document;

// Create a new PDF document
using var document = PdfTickleSharp.Core.PdfTickleSharp.CreateDocument("My Document", "Author Name");

// Add pages with different sizes
var page1 = document.AddPage(PdfPageSize.A4);
var page2 = document.AddPage(PdfPageSize.Letter);

// Add text content
page1.AddText("Hello, PdfTickleSharp!", 100, 750);
page1.AddText("This is page 1 content.", 100, 720);

page2.AddText("This is page 2 content.", 100, 720);

// Save the document
document.Save("output.pdf");

// Or get as byte array
byte[] pdfData = document.ToByteArray();

// Or save to stream
using var stream = new MemoryStream();
document.Save(stream);
```

## 🧪 Testing

The project includes a comprehensive test suite that validates all functionality:

### Master Test Runner
```bash
cd src/PdfTickleSharp.TestApp

# Run all available phases
dotnet run

# Run specific phase
dotnet run 1        # Phase 1: Foundation
dotnet run 2        # Phase 2: Core Features (when available)
```

```bash
# Validate a previously generated phase2.pdf
dotnet run test
```

### Test Output
- **Comprehensive validation** of all Phase 1 and Phase 2 features
- **Structural PDF validation** — the output file is parsed and its streams
  decompressed, so the checks test the real document, not a text search
- **Multi-page testing** with different page sizes
- **I/O testing** (file, stream, byte array)
- **Clear pass/fail reporting**, with a non-zero exit code when a check fails

## 🛠️ Development Roadmap

### Phase 1: Foundation ✅ **COMPLETE**
- [x] Project structure setup
- [x] Basic solution architecture  
- [x] Core PDF document model
- [x] Basic text rendering API
- [x] Simple PDF creation (file I/O)

**Status**: All 5 foundation elements are working and tested. Basic PDFs can be created with text content across multiple pages and page sizes.

### Phase 2: Core Features ✅ **COMPLETE**
**Priority Fixes from Phase 1:**
- [x] Fix Unicode/UTF-8 encoding (now supports special characters and symbols)
- [x] Advanced text formatting (fonts, sizes, colors, styles)
- [x] Text layout improvements (alignment, spacing, flow)

**New Features:**
- [x] Image insertion and manipulation
- [x] Enhanced metadata handling
- [x] Basic drawing operations (lines, shapes, circles)
- [x] Improved coordinate system and positioning

### Phase 3: Advanced Features 🚧 **PLANNED**
- [ ] PDF annotations
- [ ] Digital signatures
- [ ] Form field handling
- [ ] PDF/A compliance
- [ ] Security and encryption
- [ ] Colour emoji (translate COLRv1 paint graphs to PDF shading patterns and soft masks)

### Phase 4: Polish & Performance 🚧 **PLANNED**
- [ ] Performance optimizations
- [ ] Memory usage improvements
- [ ] Comprehensive documentation
- [ ] Extended sample applications

## 🧭 Coordinate System

X and Y are in points (1/72 inch) and measured from the **bottom-left corner** of
the page, matching PDF's own coordinate system. A larger Y is higher up the page.
On A4 (595 × 842 pt), `y = 750` is near the top and `y = 50` near the bottom.

- **Text**: the given point is the start of the text baseline.
- **Rectangles and images**: the given point is the bottom-left corner.
- **Circles**: the given point is the centre.

Elements are painted in the order they are added, so later elements draw over
earlier ones.

## 🔤 How Text and Fonts Work

Text is rendered with TrueType fonts that are **subsetted and embedded** into every
PDF, so output looks identical everywhere and does not depend on fonts installed on
the reader's machine. Each font is encoded as an Identity-H CID font with a
`/ToUnicode` map, which keeps the text selectable, copyable and searchable.

Requested font families map onto the bundled DejaVu faces:

| Requested family | Rendered with | Styles available |
|---|---|---|
| `Helvetica`, `Arial`, `sans-serif` (default) | DejaVu Sans | regular, bold, italic, bold-italic |
| `Times`, `Times-Roman`, `serif`, `Georgia` | DejaVu Serif | regular, bold, italic, bold-italic |
| `Courier`, `monospace`, `Consolas`, `Menlo` | DejaVu Sans Mono | regular, bold, italic, bold-italic |

Only the glyphs a document actually uses are embedded, so a single-line PDF is
about 5 KB rather than carrying a 739 KB font file.

If a character is missing from the chosen family, the writer falls back to another
bundled font for just that character rather than dropping it, splitting the line
into runs behind the scenes. Emoji reach **Noto Emoji** this way, so text can mix
words and emoji freely without the caller doing anything.

Text width is measured with the font's real advance widths, so alignment and word
wrapping are accurate rather than estimated.

## ⚠️ Known Limitations

- **Emoji render in monochrome, not colour.** Characters such as 🌍, 🚀 and 🎉 are
  drawn as black outlines from Noto Emoji. Colour emoji are not supported: the
  bundled `NotoColorEmoji` is a COLRv1 font whose artwork is a paint graph of
  affine transforms, linear and radial gradients and compositing operations, with
  empty `glyf` outlines for the emoji code points. Translating that graph into PDF
  shading patterns and soft masks is a project in its own right and is left to a
  later phase. Anything a bundled font cannot render at all falls back to a visible
  placeholder box; use `FontManager.CanRender(text)` to check text up front.
- **Font substitution.** "Helvetica" and "Times" are rendered with DejaVu Sans and
  DejaVu Serif. Metrics are exact for the font actually used, but the letterforms
  are not identical to the fonts they stand in for.
- **No automatic page overflow.** Text that runs past the bottom of a page is not
  moved to a new page; page breaks are the caller's responsibility.
- **Reading existing PDFs is not supported.** The library writes PDFs only.

## ✅ Completed Features

### Phase 1: Foundation ✅
- ✅ **PDF Creation**: Generate PDFs from scratch with text positioning
- ✅ **Document Model**: Complete PDF document structure with pages and metadata
- ✅ **Page Management**: Multiple page sizes (A4, Letter, A3, Custom) and multi-page support
- ✅ **File I/O Operations**: Save to file, stream, or byte array

### Phase 2: Core Features ✅
- ✅ **Unicode Support**: Rendered from embedded fonts and searchable via `/ToUnicode`
- ✅ **Advanced Text Formatting**: Font families, sizes, colors, bold and italic
- ✅ **Text Layout**: Alignment and word wrapping using real font metrics
- ✅ **Drawing Operations**: Lines, rectangles, circles with fill options
- ✅ **Image Insertion**: PNG and JPEG, including PNG alpha transparency
- ✅ **Enhanced Metadata**: Written to the PDF `/Info` dictionary

### Verified Output
Generated PDFs are checked by a structural validator (`dotnet run test`) that parses
the file rather than searching its bytes. It verifies the cross-reference table
points at the right objects, that every indirect reference resolves, that embedded
font programs are valid TrueType data, that image XObjects carry complete decode
parameters, and that content streams decompress with balanced `BT`/`ET` and `q`/`Q`.

## 🤝 Contributing

We welcome contributions from the community! Whether you're fixing bugs, adding features, or improving documentation, your help is appreciated.

### How to Contribute

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions and style guidelines
- Run the test suite (`dotnet run` in PdfTickleSharp.TestApp) to ensure your changes work
- Update documentation as needed
- Ensure all tests pass before submitting PR

## 📋 Requirements

- **Target Framework**: .NET 6.0+
- **Language**: C# 10+
- **Dependencies**: Minimal external dependencies (by design)
- **Platforms**: Windows, macOS, Linux

## 📖 Documentation

- [API Documentation](docs/api/) - Detailed API reference (coming soon)
- [Getting Started Guide](docs/getting-started.md) - Step-by-step tutorial (coming soon)
- [Examples](USAGE_EXAMPLE.cs) - Sample applications and use cases
- [Architecture Overview](docs/architecture.md) - Technical design decisions (coming soon)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by the need for affordable PDF solutions in the .NET ecosystem
- Built with modern .NET practices and cross-platform compatibility in mind
- Community-driven development approach

## 📞 Support

- **Issues**: Report bugs and request features via [GitHub Issues](https://github.com/yourusername/PdfTickleSharp/issues)
- **Discussions**: Join community discussions in [GitHub Discussions](https://github.com/yourusername/PdfTickleSharp/discussions)
- **Documentation**: Visit our [documentation site](https://pdfticklesharp.dev) for guides and tutorials

---

**Made with ❤️ by the open-source community**

*PdfTickleSharp - Because PDF manipulation shouldn't cost a fortune!*

**Phase 1 & 2 Complete! 🎉 Ready for Phase 3 development.** 