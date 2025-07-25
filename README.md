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

### Core Features (PdfTickleSharp.Core) - Phase 2 Planned 🚧
- 🚧 **Advanced Text Formatting**: Multiple fonts, sizes, colors, and styles
- 🚧 **Unicode Support**: Fix UTF-8 encoding for special characters
- 🚧 **Image Insertion**: Add images to PDF documents
- 🚧 **Enhanced Layout**: Text flow, alignment, and spacing
- 🚧 **Basic Drawing**: Lines, shapes, and simple graphics

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
│   │   │   └── PdfMetadata.cs               # Document metadata
│   │   ├── IO/                              # File I/O operations ✅
│   │   │   └── PdfWriter.cs                 # PDF file generation
│   │   ├── Graphics/                         # Drawing and rendering (future)
│   │   ├── Text/                            # Text processing (future)
│   │   └── PdfTickleSharp.cs                # Main library entry point
│   ├── PdfTickleSharp.Advanced/              # Advanced features (future)
│   │   └── PdfTickleSharp.Advanced.csproj
│   └── PdfTickleSharp.TestApp/              # Comprehensive test suite ✅
│       ├── PdfTickleSharp.TestApp.csproj
│       ├── Program.cs                       # Master test runner
│       └── Phase1.cs                        # Phase 1 specific tests
├── PdfTest/                                  # Additional test project
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

### Test Output
- **Comprehensive validation** of all Phase 1 features
- **PDF file generation** with verification
- **Multi-page testing** with different page sizes
- **I/O testing** (file, stream, byte array)
- **Clear pass/fail reporting** with detailed output

## 🛠️ Development Roadmap

### Phase 1: Foundation ✅ **COMPLETE**
- [x] Project structure setup
- [x] Basic solution architecture  
- [x] Core PDF document model
- [x] Basic text rendering API
- [x] Simple PDF creation (file I/O)

**Status**: All 5 foundation elements are working and tested. Basic PDFs can be created with text content across multiple pages and page sizes.

### Phase 2: Core Features 🚧 **IN PROGRESS**
**Priority Fixes from Phase 1:**
- [ ] Fix Unicode/UTF-8 encoding (currently ASCII only)
- [ ] Advanced text formatting (fonts, sizes, colors, styles)
- [ ] Text layout improvements (alignment, spacing, flow)

**New Features:**
- [ ] Image insertion and manipulation
- [ ] Enhanced metadata handling
- [ ] Basic drawing operations (lines, shapes)
- [ ] Improved coordinate system and positioning

### Phase 3: Advanced Features 🚧 **PLANNED**
- [ ] PDF annotations
- [ ] Digital signatures
- [ ] Form field handling
- [ ] PDF/A compliance
- [ ] Security and encryption

### Phase 4: Polish & Performance 🚧 **PLANNED**
- [ ] Performance optimizations
- [ ] Memory usage improvements
- [ ] Comprehensive documentation
- [ ] Extended sample applications

## ⚠️ Known Limitations (Phase 1)

While Phase 1 is functionally complete, there are known limitations that will be addressed in Phase 2:

### Text Rendering
- **Unicode Issue**: Special characters (✅, ℃, etc.) display as '?' due to ASCII encoding
- **Font Limitations**: Only Helvetica 12pt available
- **No Text Styling**: No bold, italic, colors, or size variations
- **Basic Positioning**: Simple X,Y coordinates only

### What Works Well
- ✅ **Page Management**: A4, Letter, A3, and custom page sizes
- ✅ **Multi-page Documents**: Reliable page addition and management  
- ✅ **File I/O**: All save methods (file, stream, byte array) working perfectly
- ✅ **Document Structure**: Complete PDF document model with metadata

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

**Phase 1 Complete! 🎉 Ready for Phase 2 development.** 