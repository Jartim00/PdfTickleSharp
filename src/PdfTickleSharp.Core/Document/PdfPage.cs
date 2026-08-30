using PdfTickleSharp.Core.Text;
using PdfTickleSharp.Core.Graphics;

namespace PdfTickleSharp.Core.Document;

/// <summary>
/// Represents a single page in a PDF document.
/// </summary>
public class PdfPage
{
    private readonly List<PageContent> _contents;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public PdfPageSize PageSize { get; }

    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int PageNumber { get; internal set; }

    /// <summary>
    /// Gets the parent document.
    /// </summary>
    public PdfDocument? Document { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the PdfPage class.
    /// </summary>
    /// <param name="pageSize">The page size.</param>
    public PdfPage(PdfPageSize pageSize)
    {
        PageSize = pageSize ?? throw new ArgumentNullException(nameof(pageSize));
        _contents = new List<PageContent>();
    }

    /// <summary>
    /// Adds text to the page at the specified position with default formatting.
    /// </summary>
    /// <param name="text">The text to add.</param>
    /// <param name="x">The X coordinate in points.</param>
    /// <param name="y">The Y coordinate in points.</param>
    public void AddText(string text, double x, double y)
    {
        AddText(text, x, y, new TextFormat());
    }

    /// <summary>
    /// Adds text to the page at the specified position with custom formatting.
    /// </summary>
    /// <param name="text">The text to add.</param>
    /// <param name="x">The X coordinate in points.</param>
    /// <param name="y">The Y coordinate in points.</param>
    /// <param name="format">The text formatting options.</param>
    public void AddText(string text, double x, double y, TextFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(format);

        _contents.Add(new TextContent
        {
            Text = text,
            X = x,
            Y = y,
            Format = format.Clone()
        });
    }

    /// <summary>
    /// Adds flowing text to the page with automatic line wrapping.
    /// </summary>
    /// <param name="text">The text to add.</param>
    /// <param name="x">The X coordinate in points.</param>
    /// <param name="y">The Y coordinate in points.</param>
    /// <param name="width">The maximum width for text wrapping.</param>
    /// <param name="format">The text formatting options.</param>
    public void AddTextFlow(string text, double x, double y, double width, TextFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(format);

        _contents.Add(new TextFlowContent
        {
            Text = text,
            X = x,
            Y = y,
            Width = width,
            Format = format.Clone()
        });
    }

    /// <summary>
    /// Adds an image to the page.
    /// </summary>
    /// <param name="imageData">The image data as bytes.</param>
    /// <param name="x">The X coordinate in points.</param>
    /// <param name="y">The Y coordinate in points.</param>
    /// <param name="width">The width of the image in points.</param>
    /// <param name="height">The height of the image in points.</param>
    public void AddImage(byte[] imageData, double x, double y, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        _contents.Add(new ImageContent
        {
            ImageData = imageData,
            X = x,
            Y = y,
            Width = width,
            Height = height
        });
    }

    /// <summary>
    /// Draws a line on the page.
    /// </summary>
    /// <param name="x1">The starting X coordinate in points.</param>
    /// <param name="y1">The starting Y coordinate in points.</param>
    /// <param name="x2">The ending X coordinate in points.</param>
    /// <param name="y2">The ending Y coordinate in points.</param>
    /// <param name="color">The line color.</param>
    /// <param name="width">The line width in points.</param>
    public void DrawLine(double x1, double y1, double x2, double y2, Color color, double width = 1.0)
    {
        ArgumentNullException.ThrowIfNull(color);

        _contents.Add(new LineContent
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Color = color,
            Width = width
        });
    }

    /// <summary>
    /// Draws a rectangle on the page.
    /// </summary>
    /// <param name="x">The X coordinate in points.</param>
    /// <param name="y">The Y coordinate in points.</param>
    /// <param name="width">The width in points.</param>
    /// <param name="height">The height in points.</param>
    /// <param name="color">The rectangle color.</param>
    /// <param name="lineWidth">The line width in points.</param>
    /// <param name="filled">Whether the rectangle should be filled.</param>
    public void DrawRectangle(double x, double y, double width, double height, Color color, double lineWidth = 1.0, bool filled = false)
    {
        ArgumentNullException.ThrowIfNull(color);

        _contents.Add(new RectangleContent
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Color = color,
            LineWidth = lineWidth,
            Filled = filled
        });
    }

    /// <summary>
    /// Draws a circle on the page.
    /// </summary>
    /// <param name="centerX">The center X coordinate in points.</param>
    /// <param name="centerY">The center Y coordinate in points.</param>
    /// <param name="radius">The radius in points.</param>
    /// <param name="color">The circle color.</param>
    /// <param name="lineWidth">The line width in points.</param>
    /// <param name="filled">Whether the circle should be filled.</param>
    public void DrawCircle(double centerX, double centerY, double radius, Color color, double lineWidth = 1.0, bool filled = false)
    {
        ArgumentNullException.ThrowIfNull(color);

        _contents.Add(new CircleContent
        {
            CenterX = centerX,
            CenterY = centerY,
            Radius = radius,
            Color = color,
            LineWidth = lineWidth,
            Filled = filled
        });
    }

    /// <summary>
    /// Gets all content elements on this page.
    /// </summary>
    /// <returns>An enumerable of page content elements.</returns>
    internal IEnumerable<PageContent> GetContents()
    {
        return _contents.AsReadOnly();
    }

    public override string ToString()
    {
        return $"Page {PageNumber} ({PageSize.Name}) - {_contents.Count} content elements";
    }
} 