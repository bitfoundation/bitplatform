namespace Bit.Bink;

/// <summary>Options for PDF export.</summary>
public sealed record BinkPdfExportOptions : BinkExportOptions
{
    /// <summary>Default options: a document unit is a CSS pixel, three quarters of a point.</summary>
    public static readonly BinkPdfExportOptions Default = new();

    /// <summary>
    /// PDF points (1/72 inch) per document unit. The default, 0.75, treats a unit as a CSS pixel, so a page 800 units
    /// wide prints 600 points (8.33 inches) wide, the size a browser prints it at. A page whose longer side would
    /// pass <see cref="BinkPdfExporter.MaxPageSize"/> is scaled down to it, keeping its proportions.
    /// </summary>
    public float Scale { get; init; } = 0.75f;

    /// <summary>Title written into the file's properties, which a viewer shows in its title bar.</summary>
    public string? Title { get; init; }

    /// <summary>Subject written into the file's properties: what the drawing shows, for search and assistive technology.</summary>
    public string? Description { get; init; }

    /// <summary>Author written into the file's properties.</summary>
    public string? Author { get; init; }

    /// <summary>The natural language of the content, such as <c>en-US</c>, which a screen reader uses to pronounce it.</summary>
    public string? Language { get; init; }

    /// <summary>
    /// The most pixels one embedded image may decode to. Images come from the document, which may be untrusted, and a
    /// few bytes can claim an enormous picture; a larger one is skipped and reported in
    /// <see cref="BinkPdfExportResult.SkippedImages"/>. JPEG images are copied into the file without decoding.
    /// </summary>
    public long MaxImagePixels { get; init; } = 64_000_000;

    /// <summary>Whether content streams are compressed. Off only to read the file in a text editor.</summary>
    public bool Compress { get; init; } = true;
}
