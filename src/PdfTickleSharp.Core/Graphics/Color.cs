namespace PdfTickleSharp.Core.Graphics;

/// <summary>
/// Represents a color in RGB format for PDF rendering.
/// </summary>
public class Color
{
    /// <summary>
    /// Gets the red component (0-255).
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green component (0-255).
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue component (0-255).
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Initializes a new instance of the Color class.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Initializes a new instance of the Color class from RGB values.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    public Color(int r, int g, int b)
    {
        R = (byte)Math.Clamp(r, 0, 255);
        G = (byte)Math.Clamp(g, 0, 255);
        B = (byte)Math.Clamp(b, 0, 255);
    }

    /// <summary>
    /// Gets the normalized red component (0.0-1.0).
    /// </summary>
    public double NormalizedR => R / 255.0;

    /// <summary>
    /// Gets the normalized green component (0.0-1.0).
    /// </summary>
    public double NormalizedG => G / 255.0;

    /// <summary>
    /// Gets the normalized blue component (0.0-1.0).
    /// </summary>
    public double NormalizedB => B / 255.0;

    /// <summary>
    /// Black color.
    /// </summary>
    public static Color Black => new Color(0, 0, 0);

    /// <summary>
    /// White color.
    /// </summary>
    public static Color White => new Color(255, 255, 255);

    /// <summary>
    /// Red color.
    /// </summary>
    public static Color Red => new Color(255, 0, 0);

    /// <summary>
    /// Green color.
    /// </summary>
    public static Color Green => new Color(0, 255, 0);

    /// <summary>
    /// Blue color.
    /// </summary>
    public static Color Blue => new Color(0, 0, 255);

    /// <summary>
    /// Yellow color.
    /// </summary>
    public static Color Yellow => new Color(255, 255, 0);

    /// <summary>
    /// Cyan color.
    /// </summary>
    public static Color Cyan => new Color(0, 255, 255);

    /// <summary>
    /// Magenta color.
    /// </summary>
    public static Color Magenta => new Color(255, 0, 255);

    /// <summary>
    /// Orange color.
    /// </summary>
    public static Color Orange => new Color(255, 165, 0);

    /// <summary>
    /// Purple color.
    /// </summary>
    public static Color Purple => new Color(128, 0, 128);

    /// <summary>
    /// Gray color.
    /// </summary>
    public static Color Gray => new Color(128, 128, 128);

    /// <summary>
    /// Light gray color.
    /// </summary>
    public static Color LightGray => new Color(192, 192, 192);

    /// <summary>
    /// Dark gray color.
    /// </summary>
    public static Color DarkGray => new Color(64, 64, 64);

    /// <summary>
    /// Returns a string representation of the color.
    /// </summary>
    /// <returns>A string in the format "RGB(R, G, B)".</returns>
    public override string ToString()
    {
        return $"RGB({R}, {G}, {B})";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current color.
    /// </summary>
    /// <param name="obj">The object to compare with the current color.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is Color other)
        {
            return R == other.R && G == other.G && B == other.B;
        }
        return false;
    }

    /// <summary>
    /// Returns the hash code for this color.
    /// </summary>
    /// <returns>A hash code for the current color.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B);
    }

    /// <summary>
    /// Equality operator for colors.
    /// </summary>
    /// <param name="left">The left color.</param>
    /// <param name="right">The right color.</param>
    /// <returns>True if the colors are equal; otherwise, false.</returns>
    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator for colors.
    /// </summary>
    /// <param name="left">The left color.</param>
    /// <param name="right">The right color.</param>
    /// <returns>True if the colors are not equal; otherwise, false.</returns>
    public static bool operator !=(Color left, Color right)
    {
        return !left.Equals(right);
    }
} 