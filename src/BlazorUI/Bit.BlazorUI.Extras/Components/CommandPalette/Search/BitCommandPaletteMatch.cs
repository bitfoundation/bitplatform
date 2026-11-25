namespace Bit.BlazorUI;

/// <summary>Result of matching a query against a <see cref="BitCommandPaletteCommand"/> across its searchable fields.</summary>
public sealed record BitCommandPaletteMatch(BitCommandPaletteCommand Command, double Score, int[] TitlePositions, int[] SubtitlePositions, string? MatchedKeyword)
{
    /// <summary>Final rank after priority and frecency adjustments. Set by the ranker.</summary>
    public double Rank { get; init; }

    /// <summary>Frecency contribution included in <see cref="Rank"/>.</summary>
    public double Frecency { get; init; }

    /// <summary>True when the command came from an async <see cref="IBitCommandPaletteSource"/>.</summary>
    public bool FromSource { get; init; }

    /// <summary>The name of the source the command came from, when <see cref="FromSource"/>.</summary>
    public string? SourceName { get; init; }

    /// <summary>
    /// The titles of the pages a nested command lives on, outermost first, when it is listed on the root page (see
    /// <see cref="BitCommandPaletteOptions.SearchNestedCommands"/>); empty otherwise. Rendered before the title: "Theme › Dark".
    /// </summary>
    public IReadOnlyList<string> Path { get; init; } = [];

    /// <summary>True when the command was only found by allowing typos (see <see cref="BitCommandPaletteOptions.TypoTolerance"/>).</summary>
    public bool IsApproximate { get; init; }

    /// <summary>The group of a nested command's outermost ancestor, which it is listed under when it has none of its own.</summary>
    internal string? InheritedGroup { get; init; }
}
