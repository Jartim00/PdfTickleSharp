namespace PdfTickleSharp.Core.Document;

/// <summary>
/// Represents metadata information for a PDF document.
/// </summary>
public class PdfMetadata
{
    /// <summary>
    /// Gets or sets the title of the document.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the author of the document.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the producer library.
    /// </summary>
    public string Producer { get; set; }

    /// <summary>
    /// Gets or sets the creation date of the document.
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the modification date of the document.
    /// </summary>
    public DateTime ModificationDate { get; set; }

    /// <summary>
    /// Initializes a new instance of the PdfMetadata class.
    /// </summary>
    public PdfMetadata()
    {
        Producer = "PdfTickleSharp";
        CreationDate = DateTime.UtcNow;
        ModificationDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the modification date to the current time.
    /// </summary>
    public void UpdateModificationDate()
    {
        ModificationDate = DateTime.UtcNow;
    }
} 