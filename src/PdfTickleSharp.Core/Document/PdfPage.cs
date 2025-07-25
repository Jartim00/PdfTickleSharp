namespace PdfTickleSharp.Core.Document;

/// <summary>
/// Represents a single page in a PDF document.
/// </summary>
public class PdfPage
{
    private readonly List<string> _contents;

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
        _contents = new List<string>();
    }

    /// <summary>
    /// Adds text to the page at the specified position.
    /// </summary>
    /// <param name="text">The text to add.</param>
    /// <param name="x">The X coordinate in points.</param>
    /// <param name="y">The Y coordinate in points.</param>
    public void AddText(string text, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(text);
        _contents.Add($"Text: '{text}' at ({x}, {y})");
    }

    public override string ToString()
    {
        return $"Page {PageNumber} ({PageSize.Name}) - {_contents.Count} content elements";
    }
} 