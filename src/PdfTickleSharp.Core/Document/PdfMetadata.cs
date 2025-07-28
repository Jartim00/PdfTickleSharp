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
    /// Gets or sets the subject of the document.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the keywords for the document.
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// Gets or sets the creator application.
    /// </summary>
    public string? Creator { get; set; }

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
        Creator = "PdfTickleSharp Library";
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

    /// <summary>
    /// Returns a string representation of the metadata.
    /// </summary>
    /// <returns>A string describing the metadata.</returns>
    public override string ToString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(Title))
            parts.Add($"Title: {Title}");
        if (!string.IsNullOrEmpty(Author))
            parts.Add($"Author: {Author}");
        if (!string.IsNullOrEmpty(Subject))
            parts.Add($"Subject: {Subject}");
        if (!string.IsNullOrEmpty(Keywords))
            parts.Add($"Keywords: {Keywords}");
        if (!string.IsNullOrEmpty(Creator))
            parts.Add($"Creator: {Creator}");
        if (!string.IsNullOrEmpty(Producer))
            parts.Add($"Producer: {Producer}");
        
        return string.Join(", ", parts);
    }
} 