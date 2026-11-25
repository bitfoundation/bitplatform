namespace Bit.BlazorUI;

/// <summary>
/// One page of an <see cref="IBitCommandPaletteSource"/>'s results (see <see cref="IBitCommandPaletteSource.SearchPageAsync"/>): the commands,
/// the cursor of the page after it - null when there is none - and, when the source knows it, how many results there
/// are in all, which the "Show 37 more…" row counts down from.
/// </summary>
public sealed record BitCommandPaletteSourcePage(IReadOnlyList<BitCommandPaletteCommand> Commands, string? NextCursor = null, int? TotalCount = null)
{
    /// <summary>Whether there is a page after this one.</summary>
    public bool HasMore => NextCursor is not null;
}
