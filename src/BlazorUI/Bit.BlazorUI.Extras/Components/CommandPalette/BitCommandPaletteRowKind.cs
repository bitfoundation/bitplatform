namespace Bit.BlazorUI;

/// <summary>Kind of a rendered row.</summary>
public enum BitCommandPaletteRowKind
{
    GroupHeader,
    Item,

    /// <summary>Never rendered: a listbox may own only options and groups, and separators between groups are their headings.</summary>
    [Obsolete("The palette never renders separators: a WAI-ARIA listbox may own only options and groups.")]
    Separator,
}
