// Tagged-PDF logical structure tree (/StructTreeRoot) exposure. Assistive
// technology and reflow tools use this to recover reading order, headings and
// alternate text. This parses the structure hierarchy into a simple tree; it
// does not resolve marked-content (MCID) leaves, which reference page content.

namespace Bit.BlazorUI;

/// <summary>A node in the tagged-PDF logical structure tree.</summary>
public sealed class BitPdfStructElement
{
    /// <summary>The structure type (e.g. "Document", "H1", "P", "Figure").</summary>
    public required string Type { get; init; }

    /// <summary>Alternate description text (<c>/Alt</c>), when supplied.</summary>
    public string? Alt { get; init; }

    /// <summary>Replacement text (<c>/ActualText</c>), when supplied.</summary>
    public string? ActualText { get; init; }

    /// <summary>Child structure elements (marked-content leaves are omitted).</summary>
    public IReadOnlyList<BitPdfStructElement> Children { get; init; } = Array.Empty<BitPdfStructElement>();
}
