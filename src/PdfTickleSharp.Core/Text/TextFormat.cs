using PdfTickleSharp.Core.Graphics;

namespace PdfTickleSharp.Core.Text;

/// <summary>
/// Defines text formatting options for PDF text rendering.
/// </summary>
public class TextFormat
{
    /// <summary>
    /// Gets or sets the font family name.
    /// </summary>
    public string FontFamily { get; set; } = "Helvetica";

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public double FontSize { get; set; } = 12.0;

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    public Color Color { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets whether the text should be bold.
    /// </summary>
    public bool IsBold { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the text should be italic.
    /// </summary>
    public bool IsItalic { get; set; } = false;

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Gets or sets the line spacing multiplier.
    /// </summary>
    public double LineSpacing { get; set; } = 1.0;

    /// <summary>
    /// Initializes a new instance of the TextFormat class with default values.
    /// </summary>
    public TextFormat()
    {
    }

    /// <summary>
    /// Initializes a new instance of the TextFormat class with specified values.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size in points.</param>
    /// <param name="color">The text color.</param>
    public TextFormat(string fontFamily, double fontSize = 12.0, Color? color = null)
    {
        FontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily));
        FontSize = fontSize;
        Color = color ?? Color.Black;
    }

    /// <summary>
    /// Creates a copy of this TextFormat with modified properties.
    /// </summary>
    /// <returns>A new TextFormat instance with the same properties.</returns>
    public TextFormat Clone()
    {
        return new TextFormat
        {
            FontFamily = this.FontFamily,
            FontSize = this.FontSize,
            Color = this.Color,
            IsBold = this.IsBold,
            IsItalic = this.IsItalic,
            Alignment = this.Alignment,
            LineSpacing = this.LineSpacing
        };
    }

    /// <summary>
    /// Returns a string representation of the TextFormat.
    /// </summary>
    /// <returns>A string describing the text format.</returns>
    public override string ToString()
    {
        var style = IsBold && IsItalic ? "Bold Italic" :
                   IsBold ? "Bold" :
                   IsItalic ? "Italic" : "Normal";

        return $"{FontFamily} {FontSize}pt {Color} {style} {Alignment}";
    }
} 