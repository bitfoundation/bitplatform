namespace Bit.BlazorUI;

/// <summary>
/// Writes one block of a type in an export format (HTML or Markdown).
/// </summary>
/// <param name="block">The block to write.</param>
/// <param name="children">
/// The block's children, already written in the same format, or empty when it has none. A type that nests decides where
/// they go (inside a <c>details</c> element, indented under the block); one that ignores them leaves them out.
/// </param>
public delegate string BitBlockExport(BitBlock block, string children);

/// <summary>
/// A type of block a <see cref="BitBlockDocument"/> can hold: its name, what its text is, whether it nests, and how it
/// is exported. The built-in types are in <see cref="BitBlockTypes"/>; an app adds its own, and one registered under a
/// built-in's name replaces it.
/// </summary>
public sealed class BitBlockType
{
    /// <summary>The name a block's <see cref="BitBlock.Type"/> refers to the type by (<c>"paragraph"</c>).</summary>
    public required string Name { get; init; }

    /// <summary>What a block of the type holds as its text.</summary>
    public required BitBlockKind Kind { get; init; }

    /// <summary>Whether a block of the type may hold <see cref="BitBlock.Children"/>.</summary>
    public bool Nests { get; init; }

    /// <summary>The words the slash menu offers the type under, or null to keep it out of the menu.</summary>
    public string? SlashLabel { get; init; }

    /// <summary>More words the slash menu's filter finds the type by, separated by spaces.</summary>
    public string? SlashKeywords { get; init; }

    /// <summary>
    /// Writes a block of the type as HTML. The result is sanitized like every other block's. Null for an app's type
    /// writes its text as a paragraph; the built-in types write their own.
    /// </summary>
    public BitBlockExport? ToHtml { get; init; }

    /// <summary>
    /// Writes a block of the type as Markdown. Null for an app's type writes its text as a paragraph; the built-in types
    /// write their own.
    /// </summary>
    public BitBlockExport? ToMarkdown { get; init; }
}
