namespace Bit.BlazorUI;

/// <summary>What a <see cref="BitBlockType"/> holds as its <see cref="BitBlock.Text"/>.</summary>
public enum BitBlockKind
{
    /// <summary>Formatted text: inline HTML in the vocabulary <see cref="BitBlock.Text"/> describes.</summary>
    Text,

    /// <summary>Plain text, kept exactly as written, line breaks included (a code block).</summary>
    Code,

    /// <summary>No text: the block is all props (an image, a divider, an embed).</summary>
    Atom
}
