namespace PdfTickleSharp.Core.Document;

/// <summary>
/// Represents a PDF document and provides the main API for creating and manipulating PDFs.
/// </summary>
public class PdfDocument : IDisposable
{
    private readonly List<PdfPage> _pages;
    private bool _disposed;

    /// <summary>
    /// Gets the document metadata.
    /// </summary>
    public PdfMetadata Metadata { get; }

    /// <summary>
    /// Gets the pages in the document.
    /// </summary>
    public IReadOnlyList<PdfPage> Pages => _pages.AsReadOnly();

    /// <summary>
    /// Gets the number of pages in the document.
    /// </summary>
    public int PageCount => _pages.Count;

    /// <summary>
    /// Gets or sets the default page size for new pages.
    /// </summary>
    public PdfPageSize DefaultPageSize { get; set; }

    /// <summary>
    /// Initializes a new instance of the PdfDocument class.
    /// </summary>
    public PdfDocument()
    {
        _pages = new List<PdfPage>();
        Metadata = new PdfMetadata();
        DefaultPageSize = PdfPageSize.A4;
    }

    /// <summary>
    /// Adds a new page with the default page size.
    /// </summary>
    /// <returns>The newly created page.</returns>
    public PdfPage AddPage()
    {
        return AddPage(DefaultPageSize);
    }

    /// <summary>
    /// Adds a new page with the specified page size.
    /// </summary>
    /// <param name="pageSize">The page size for the new page.</param>
    /// <returns>The newly created page.</returns>
    public PdfPage AddPage(PdfPageSize pageSize)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(pageSize);

        var page = new PdfPage(pageSize)
        {
            Document = this,
            PageNumber = _pages.Count + 1
        };

        _pages.Add(page);
        Metadata.UpdateModificationDate();

        return page;
    }

    /// <summary>
    /// Saves the document to the specified file path.
    /// </summary>
    /// <param name="filePath">The file path where to save the PDF.</param>
    public void Save(string filePath)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(filePath);

        // TODO: Implement actual PDF generation
        Metadata.UpdateModificationDate();
        throw new NotImplementedException("PDF generation is not yet implemented.");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PdfDocument));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pages.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public override string ToString()
    {
        return $"PDF Document - {PageCount} pages";
    }
} 