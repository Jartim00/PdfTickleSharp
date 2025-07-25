using PdfTickleSharp.Core.Document;

namespace PdfTickleSharp.Core;

/// <summary>
/// Main entry point for PdfTickleSharp library functionality.
/// </summary>
public static class PdfTickleSharp
{
    /// <summary>
    /// Gets the version of the PdfTickleSharp library.
    /// </summary>
    public static string Version => "1.0.0-alpha";

    /// <summary>
    /// Creates a new PDF document with default settings.
    /// </summary>
    /// <returns>A new PdfDocument instance.</returns>
    public static PdfDocument CreateDocument()
    {
        return new PdfDocument();
    }

    /// <summary>
    /// Creates a new PDF document with metadata.
    /// </summary>
    /// <param name="title">Document title.</param>
    /// <param name="author">Document author.</param>
    /// <returns>A new PdfDocument instance with populated metadata.</returns>
    public static PdfDocument CreateDocument(string title, string? author = null)
    {
        var document = new PdfDocument();
        document.Metadata.Title = title;
        document.Metadata.Author = author;
        return document;
    }
} 