# Unicode Fonts for PdfTickleSharp

This directory is for embedding Unicode-supporting fonts in the PDF generation library.

## Recommended Fonts

For proper Unicode support, we recommend embedding these fonts:

### 1. DejaVu Fonts (Recommended)
- **DejaVuSans.ttf** - Sans-serif with excellent Unicode coverage
- **DejaVuSerif.ttf** - Serif with excellent Unicode coverage  
- **DejaVuSansMono.ttf** - Monospace with excellent Unicode coverage

**Download**: https://dejavu-fonts.github.io/
**License**: MIT License (free for commercial use)

### 2. Liberation Fonts (Alternative)
- **LiberationSans-Regular.ttf**
- **LiberationSerif-Regular.ttf**
- **LiberationMono-Regular.ttf**

**Download**: https://github.com/liberationfonts/liberation-fonts
**License**: SIL Open Font License (free for commercial use)

### 3. Noto Fonts (Google)
- **NotoSans-Regular.ttf**
- **NotoSerif-Regular.ttf**
- **NotoSansMono-Regular.ttf**

**Download**: https://fonts.google.com/noto
**License**: Apache License 2.0 (free for commercial use)

## How to Add Fonts

1. **Download** one of the recommended font families
2. **Place** the `.ttf` files in this `Fonts/` directory
3. **Build** the project - fonts will be automatically embedded as resources
4. **Use** the fonts in your PDF generation code

## Font Coverage

### DejaVu Fonts (Recommended)
- **Latin**: Full support for accented characters (é, ñ, ü, etc.)
- **Greek**: Complete Greek alphabet
- **Cyrillic**: Complete Cyrillic alphabet  
- **Mathematical**: Extensive math symbols (√, ∞, ∑, ∫, etc.)
- **Arrows**: Complete arrow set (←, →, ↑, ↓, etc.)
- **Symbols**: Trademark, copyright, registered symbols (™, ©, ®)
- **Currency**: Euro, Yen, Pound, etc. (€, ¥, £)
- **Emoji**: Basic emoji support (limited)

### Current Fallback System
If no Unicode fonts are embedded, the system falls back to:
1. **Character Mapping**: Unicode → ASCII equivalents
2. **Standard PDF Fonts**: Helvetica, Times, Courier (ASCII only)

## Usage Example

```csharp
// The FontManager will automatically use embedded Unicode fonts
var document = PdfTickleSharp.CreateDocument();
var page = document.AddPage();

// These will display properly with Unicode fonts
page.AddText("café résumé naïve", 100, 700);
page.AddText("√∞∑∫∂∇∈∉∩∪", 100, 650);
page.AddText("←→↑↓⇒⇔", 100, 600);
page.AddText("™©®€¥£", 100, 550);
```

## Font Substitution

The system automatically maps common font names to embedded Unicode fonts:
- `Arial` → `DejaVuSans`
- `Times` → `DejaVuSerif`  
- `Courier` → `DejaVuSansMono`
- `Helvetica` → `DejaVuSans`

## File Size Considerations

- **DejaVuSans.ttf**: ~1.2MB
- **DejaVuSerif.ttf**: ~1.3MB
- **DejaVuSansMono.ttf**: ~1.1MB

**Total**: ~3.6MB for full Unicode support

For smaller file sizes, consider:
- Using only DejaVuSans (most common use case)
- Using subset fonts (extract only needed characters)
- Using Liberation fonts (smaller than DejaVu)

## License Compliance

All recommended fonts are open source and free for commercial use:
- **DejaVu**: MIT License
- **Liberation**: SIL Open Font License  
- **Noto**: Apache License 2.0

## Next Steps

1. **Copy DejaVu fonts** from your download to this directory:
   ```bash
   # From your DejaVu download folder, copy all .ttf files to:
   src/PdfTickleSharp.Data/Fonts/
   ```

2. **Rebuild the project**:
   ```bash
   dotnet build
   ```

3. **Test Unicode support**:
   ```bash
   dotnet run --project PdfTickleSharp.TestApp unicode-font
   ```

4. **Verify proper rendering** - Unicode characters should now display correctly instead of being mapped to ASCII equivalents.

## License Compliance ✅

The DejaVu fonts are **free for commercial use** under the Bitstream Vera license. We have:
- ✅ Included license information in `LICENSE` file
- ✅ Properly credited original authors (Bitstream, DejaVu contributors)
- ✅ Fonts will be embedded as resources (not modified)
- ✅ License allows commercial use and redistribution 