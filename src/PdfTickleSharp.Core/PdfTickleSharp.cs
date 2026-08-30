using System.Reflection;
using PdfTickleSharp.Core.Document;

namespace PdfTickleSharp.Core;

/// <summary>
/// Main entry point for PdfTickleSharp library functionality.
/// </summary>
public static class PdfTickleSharp
{
    /// <summary>
    /// Gets the version of the PdfTickleSharp library.
    /// Read from the assembly so the csproj &lt;Version&gt; property stays the
    /// single place the version is defined.
    /// </summary>
    public static string Version { get; } = ReadAssemblyVersion();

    private static string ReadAssemblyVersion()
    {
        var informational = typeof(PdfTickleSharp).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
            return typeof(PdfTickleSharp).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        // The SDK appends "+<commit hash>" when source link metadata is present.
        var plus = informational.IndexOf('+');
        return plus < 0 ? informational : informational[..plus];
    }

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