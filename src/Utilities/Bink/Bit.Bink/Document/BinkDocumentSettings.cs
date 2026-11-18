
namespace Bit.Bink;

/// <summary>Page-level settings of a document.</summary>
public sealed record BinkDocumentSettings
{
    /// <summary>Default: 800 by 600 units, white background.</summary>
    public static readonly BinkDocumentSettings Default = new();

    /// <summary>Page width in document units, or 0 for an unbounded canvas.</summary>
    public float Width { get; init; } = 800f;

    /// <summary>Page height in document units, or 0 for an unbounded canvas.</summary>
    public float Height { get; init; } = 600f;

    /// <summary>Background color. Use <see cref="BinkColor.Transparent"/> for no background.</summary>
    public BinkColor Background { get; init; } = BinkColor.White;

    /// <summary>True when the page has a finite size.</summary>
    public bool HasFixedSize => Width > 0 && Height > 0;

    /// <summary>The page rectangle (empty for unbounded documents).</summary>
    public BinkRect PageRect => HasFixedSize ? new BinkRect(0, 0, Width, Height) : BinkRect.Empty;
}
