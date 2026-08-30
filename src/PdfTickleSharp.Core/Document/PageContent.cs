using PdfTickleSharp.Core.Text;
using PdfTickleSharp.Core.Graphics;

namespace PdfTickleSharp.Core.Document;

/// <summary>
/// Base class for all page content elements.
/// </summary>
public abstract class PageContent
{
    /// <summary>
    /// Gets the type of content.
    /// </summary>
    public abstract string ContentType { get; }
}

/// <summary>
/// Represents text content on a page.
/// </summary>
public class TextContent : PageContent
{
    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the X coordinate in points.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate in points.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the text formatting options.
    /// </summary>
    public TextFormat Format { get; set; } = new TextFormat();

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public override string ContentType => "Text";
}

/// <summary>
/// Represents flowing text content on a page with automatic line wrapping.
/// </summary>
public class TextFlowContent : PageContent
{
    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the X coordinate in points.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate in points.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the maximum width for text wrapping in points.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the text formatting options.
    /// </summary>
    public TextFormat Format { get; set; } = new TextFormat();

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public override string ContentType => "TextFlow";
}

/// <summary>
/// Represents image content on a page.
/// </summary>
public class ImageContent : PageContent
{
    /// <summary>
    /// Gets or sets the image data as bytes.
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the X coordinate in points.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate in points.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the image in points.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the image in points.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public override string ContentType => "Image";
}

/// <summary>
/// Represents line content on a page.
/// </summary>
public class LineContent : PageContent
{
    /// <summary>
    /// Gets or sets the starting X coordinate in points.
    /// </summary>
    public double X1 { get; set; }

    /// <summary>
    /// Gets or sets the starting Y coordinate in points.
    /// </summary>
    public double Y1 { get; set; }

    /// <summary>
    /// Gets or sets the ending X coordinate in points.
    /// </summary>
    public double X2 { get; set; }

    /// <summary>
    /// Gets or sets the ending Y coordinate in points.
    /// </summary>
    public double Y2 { get; set; }

    /// <summary>
    /// Gets or sets the line color.
    /// </summary>
    public Color Color { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the line width in points.
    /// </summary>
    public double Width { get; set; } = 1.0;

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public override string ContentType => "Line";
}

/// <summary>
/// Represents rectangle content on a page.
/// </summary>
public class RectangleContent : PageContent
{
    /// <summary>
    /// Gets or sets the X coordinate in points.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate in points.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width in points.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height in points.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the rectangle color.
    /// </summary>
    public Color Color { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the line width in points.
    /// </summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether the rectangle should be filled.
    /// </summary>
    public bool Filled { get; set; } = false;

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public override string ContentType => "Rectangle";
}

/// <summary>
/// Represents circle content on a page.
/// </summary>
public class CircleContent : PageContent
{
    /// <summary>
    /// Gets or sets the center X coordinate in points.
    /// </summary>
    public double CenterX { get; set; }

    /// <summary>
    /// Gets or sets the center Y coordinate in points.
    /// </summary>
    public double CenterY { get; set; }

    /// <summary>
    /// Gets or sets the radius in points.
    /// </summary>
    public double Radius { get; set; }

    /// <summary>
    /// Gets or sets the circle color.
    /// </summary>
    public Color Color { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the line width in points.
    /// </summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether the circle should be filled.
    /// </summary>
    public bool Filled { get; set; } = false;

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public override string ContentType => "Circle";
} 