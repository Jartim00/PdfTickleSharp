using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PdfTickleSharp.Data;

namespace PdfTickleSharp.Core.Text;

/// <summary>
/// Resolves a <see cref="TextFormat"/> to a concrete embeddable font, measures text
/// with real font metrics and splits text into runs when a single font cannot
/// render every character.
/// </summary>
public static class FontManager
{
    /// <summary>
    /// Shown in place of characters no bundled font can render, so unsupported
    /// input is visible rather than silently drawn as blank space.
    /// </summary>
    private const char ReplacementCharacter = '□'; // WHITE SQUARE

    private static readonly Dictionary<string, LoadedFont> Loaded = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    /// <summary>
    /// Maps the font families callers ask for onto the bundled font families.
    /// </summary>
    private static readonly Dictionary<string, string> FamilyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["helvetica"] = "sans", ["arial"] = "sans", ["sans"] = "sans", ["sans-serif"] = "sans",
        ["verdana"] = "sans", ["tahoma"] = "sans", ["segoe ui"] = "sans", ["dejavusans"] = "sans",
        ["times"] = "serif", ["times-roman"] = "serif", ["times new roman"] = "serif",
        ["serif"] = "serif", ["georgia"] = "serif", ["garamond"] = "serif", ["dejavuserif"] = "serif",
        ["courier"] = "mono", ["courier new"] = "mono", ["monospace"] = "mono", ["mono"] = "mono",
        ["consolas"] = "mono", ["menlo"] = "mono", ["dejavusansmono"] = "mono",
    };

    /// <summary>
    /// The bundled font file for each family and style, indexed by [family][bold, italic].
    /// DejaVu names its slanted sans and mono faces "Oblique" but its serif faces "Italic".
    /// </summary>
    private static readonly Dictionary<string, string[]> FontFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sans"] = ["DejaVuSans", "DejaVuSans-Bold", "DejaVuSans-Oblique", "DejaVuSans-BoldOblique"],
        ["serif"] = ["DejaVuSerif", "DejaVuSerif-Bold", "DejaVuSerif-Italic", "DejaVuSerif-BoldItalic"],
        ["mono"] = ["DejaVuSansMono", "DejaVuSansMono-Bold", "DejaVuSansMono-Oblique", "DejaVuSansMono-BoldOblique"],
    };

    /// <summary>
    /// Fonts consulted, in order, for characters the requested font cannot render.
    /// The DejaVu faces cover Latin, symbols, arrows and mathematics; NotoEmoji
    /// covers pictographic emoji, which no DejaVu face contains.
    /// </summary>
    private static readonly string[] FallbackChain =
        ["DejaVuSans", "DejaVuSerif", "DejaVuSansMono", "NotoEmoji-Regular"];

    /// <summary>
    /// Loads the bundled fonts. Safe to call repeatedly; only the first call does work.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        lock (SyncRoot)
        {
            if (_initialized) return;

            // Both the fonts that families resolve to and the fallback-only fonts,
            // which are never requested by name but cover what the others cannot.
            var names = FontFiles.Values
                .SelectMany(files => files)
                .Concat(FallbackChain)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var name in names)
            {
                var data = ResourceManager.GetResource(name);
                if (data == null) continue;

                try
                {
                    Loaded[name] = new LoadedFont(name, new TrueTypeFont(data));
                }
                catch (InvalidOperationException)
                {
                    // A font we cannot parse is simply not offered; resolution falls back.
                }
            }

            if (Loaded.Count == 0)
                throw new InvalidOperationException(
                    "No embeddable fonts were found. Ensure PdfTickleSharp.Data embeds the Fonts directory.");

            _initialized = true;
        }
    }

    /// <summary>
    /// Gets the names of the fonts available for embedding.
    /// </summary>
    public static string[] GetAvailableFonts()
    {
        Initialize();
        return Loaded.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Determines whether every character in the text can be rendered by some bundled font.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True when no character would be replaced.</returns>
    public static bool CanRender(string text)
    {
        Initialize();
        return EnumerateCodePoints(text).All(cp => cp is '\n' or '\r' or '\t' || FindFontFor(cp) != null);
    }

    /// <summary>
    /// Replaces characters no bundled font can render with a visible placeholder,
    /// so unsupported input degrades predictably instead of drawing blank space.
    /// </summary>
    /// <param name="text">The text to sanitise.</param>
    /// <returns>Text in which every character maps to a real glyph.</returns>
    public static string ProcessText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        Initialize();

        var result = new System.Text.StringBuilder(text.Length);
        foreach (var codePoint in EnumerateCodePoints(text))
        {
            if (codePoint is '\n' or '\r' or '\t')
            {
                result.Append((char)codePoint);
            }
            else if (FindFontFor(codePoint) != null)
            {
                result.Append(char.ConvertFromUtf32(codePoint));
            }
            else
            {
                result.Append(ReplacementCharacter);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Resolves the font that best matches the requested family and style.
    /// </summary>
    /// <param name="format">The text format describing family, weight and slant.</param>
    /// <returns>The font to draw with.</returns>
    internal static LoadedFont Resolve(TextFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        Initialize();

        var family = FamilyAliases.GetValueOrDefault(NormalizeFamily(format.FontFamily), "sans");
        var files = FontFiles[family];
        var index = (format.IsBold ? 1 : 0) + (format.IsItalic ? 2 : 0);

        // Walk back towards the regular face if the exact style is unavailable.
        foreach (var candidate in new[] { index, format.IsBold ? 1 : 2, 0 })
        {
            if (Loaded.TryGetValue(files[candidate], out var font)) return font;
        }
        return Loaded.Values.First();
    }

    /// <summary>
    /// Splits text into runs, each drawable with a single font. A run boundary
    /// appears only where the current font cannot render the next character.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="format">The requested text format.</param>
    /// <returns>The runs in reading order; empty when the text is empty.</returns>
    internal static List<TextRun> SplitIntoRuns(string text, TextFormat format)
    {
        Initialize();

        var runs = new List<TextRun>();
        if (string.IsNullOrEmpty(text)) return runs;

        var preferred = Resolve(format);
        var current = new System.Text.StringBuilder();
        LoadedFont? currentFont = null;

        foreach (var codePoint in EnumerateCodePoints(text))
        {
            LoadedFont font;
            string glyphText;

            if (preferred.Font.HasGlyph(codePoint))
            {
                font = preferred;
                glyphText = char.ConvertFromUtf32(codePoint);
            }
            else if (FindFontFor(codePoint) is { } fallback)
            {
                font = fallback;
                glyphText = char.ConvertFromUtf32(codePoint);
            }
            else
            {
                // Nothing can draw this character, so show a placeholder in a font
                // that has one rather than emitting an invisible .notdef glyph.
                glyphText = ReplacementCharacter.ToString();
                font = preferred.Font.HasGlyph(ReplacementCharacter)
                    ? preferred
                    : FindFontFor(ReplacementCharacter) ?? preferred;
            }

            if (currentFont != null && !ReferenceEquals(font, currentFont))
            {
                runs.Add(new TextRun(currentFont, current.ToString()));
                current.Clear();
            }

            currentFont = font;
            current.Append(glyphText);
        }

        if (currentFont != null && current.Length > 0)
            runs.Add(new TextRun(currentFont, current.ToString()));

        return runs;
    }

    /// <summary>
    /// Measures the rendered width of text in points, using the real advance widths
    /// of the fonts that would draw it.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="format">The format the text would be drawn with.</param>
    /// <returns>The width in points.</returns>
    public static double MeasureText(string text, TextFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (string.IsNullOrEmpty(text)) return 0;

        return SplitIntoRuns(text, format).Sum(run => run.Measure(format.FontSize));
    }

    private static LoadedFont? FindFontFor(int codePoint)
    {
        foreach (var name in FallbackChain)
        {
            if (Loaded.TryGetValue(name, out var font) && font.Font.HasGlyph(codePoint)) return font;
        }
        return Loaded.Values.FirstOrDefault(font => font.Font.HasGlyph(codePoint));
    }

    private static string NormalizeFamily(string family)
    {
        if (string.IsNullOrWhiteSpace(family)) return "sans";

        // Tolerate the PDF style suffixes callers may carry over, e.g. "Times-Bold".
        var name = family.Trim();
        foreach (var suffix in new[] { "-BoldOblique", "-BoldItalic", "-Bold", "-Oblique", "-Italic" })
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^suffix.Length];
                break;
            }
        }
        return name;
    }

    /// <summary>
    /// Enumerates Unicode code points, combining surrogate pairs so characters
    /// outside the Basic Multilingual Plane are handled as one unit.
    /// </summary>
    private static IEnumerable<int> EnumerateCodePoints(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                yield return char.ConvertToUtf32(text[i], text[i + 1]);
                i++;
            }
            else
            {
                yield return text[i];
            }
        }
    }
}

/// <summary>
/// A parsed font together with the identity PDF uses to refer to it.
/// </summary>
internal sealed class LoadedFont
{
    /// <summary>Gets the font's name, used as the PDF resource key.</summary>
    public string Name { get; }

    /// <summary>Gets the parsed font.</summary>
    public TrueTypeFont Font { get; }

    public LoadedFont(string name, TrueTypeFont font)
    {
        Name = name;
        Font = font;
    }

    /// <summary>
    /// Converts text to the glyph indices an Identity-H encoded PDF string needs.
    /// </summary>
    public List<ushort> GetGlyphs(string text) =>
        GetGlyphMappings(text).Select(mapping => mapping.Glyph).ToList();

    /// <summary>
    /// Pairs each glyph with the text it was produced from, which is what a
    /// ToUnicode map needs to make the drawn text searchable again.
    /// </summary>
    public IEnumerable<(ushort Glyph, string Source)> GetGlyphMappings(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            // Surrogate pairs encode one character in two UTF-16 units and must
            // stay together; a lone surrogate is passed through as-is.
            var isPair = char.IsHighSurrogate(text[i])
                         && i + 1 < text.Length
                         && char.IsLowSurrogate(text[i + 1]);

            var codePoint = isPair ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
            var source = text.Substring(i, isPair ? 2 : 1);

            if (isPair) i++;

            yield return (Font.GetGlyphIndex(codePoint), source);
        }
    }

    /// <summary>
    /// Measures text width in points at the given size.
    /// </summary>
    public double MeasureText(string text, double fontSize) =>
        GetGlyphs(text).Sum(glyph => Font.GetAdvanceWidth(glyph)) * fontSize / 1000.0;
}

/// <summary>
/// A stretch of text that a single font can render.
/// </summary>
internal sealed class TextRun
{
    /// <summary>Gets the font that renders this run.</summary>
    public LoadedFont Font { get; }

    /// <summary>Gets the run's text.</summary>
    public string Text { get; }

    public TextRun(LoadedFont font, string text)
    {
        Font = font;
        Text = text;
    }

    /// <summary>Measures this run's width in points at the given size.</summary>
    public double Measure(double fontSize) => Font.MeasureText(Text, fontSize);
}
