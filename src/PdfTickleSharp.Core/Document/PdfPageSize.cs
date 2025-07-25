namespace PdfTickleSharp.Core.Document;

/// <summary>
/// Represents standard PDF page sizes with their dimensions in points (1/72 inch).
/// </summary>
public sealed class PdfPageSize
{
    /// <summary>
    /// Gets the width of the page in points.
    /// </summary>
    public double Width { get; }
    
    /// <summary>
    /// Gets the height of the page in points.
    /// </summary>
    public double Height { get; }
    
    /// <summary>
    /// Gets the name of the page size.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the PdfPageSize class.
    /// </summary>
    /// <param name="name">The name of the page size.</param>
    /// <param name="width">The width in points.</param>
    /// <param name="height">The height in points.</param>
    public PdfPageSize(string name, double width, double height)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates a custom page size.
    /// </summary>
    /// <param name="width">The width in points.</param>
    /// <param name="height">The height in points.</param>
    /// <returns>A new PdfPageSize instance.</returns>
    public static PdfPageSize Custom(double width, double height)
    {
        return new PdfPageSize("Custom", width, height);
    }

    /// <summary>
    /// Gets a rotated version of this page size (width and height swapped).
    /// </summary>
    /// <returns>A new PdfPageSize with swapped dimensions.</returns>
    public PdfPageSize Rotate()
    {
        return new PdfPageSize($"{Name} (Rotated)", Height, Width);
    }

    // Standard page sizes
    
    /// <summary>
    /// A4 page size (210 × 297 mm).
    /// </summary>
    public static readonly PdfPageSize A4 = new("A4", 595.28, 841.89);
    
    /// <summary>
    /// A3 page size (297 × 420 mm).
    /// </summary>
    public static readonly PdfPageSize A3 = new("A3", 841.89, 1190.55);
    
    /// <summary>
    /// A5 page size (148 × 210 mm).
    /// </summary>
    public static readonly PdfPageSize A5 = new("A5", 419.53, 595.28);
    
    /// <summary>
    /// US Letter page size (8.5 × 11 inches).
    /// </summary>
    public static readonly PdfPageSize Letter = new("Letter", 612, 792);
    
    /// <summary>
    /// US Legal page size (8.5 × 14 inches).
    /// </summary>
    public static readonly PdfPageSize Legal = new("Legal", 612, 1008);
    
    /// <summary>
    /// US Tabloid page size (11 × 17 inches).
    /// </summary>
    public static readonly PdfPageSize Tabloid = new("Tabloid", 792, 1224);

    public override string ToString()
    {
        return $"{Name} ({Width:F0} × {Height:F0} pts)";
    }

    public override bool Equals(object? obj)
    {
        return obj is PdfPageSize other && 
               Math.Abs(Width - other.Width) < 0.01 && 
               Math.Abs(Height - other.Height) < 0.01;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }
} 