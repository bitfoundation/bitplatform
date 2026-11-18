namespace Bit.Bheadless;

/// <summary>How items of a <see cref="BitBhTree{TValue}"/> can be selected.</summary>
public enum BitBhTreeSelectionMode
{
    /// <summary>No selection; items can only be focused and activated.</summary>
    None,

    /// <summary>One selected item (<c>aria-selected</c>).</summary>
    Single,

    /// <summary>Many selected items (<c>aria-multiselectable</c>); Ctrl / Shift extend the selection.</summary>
    Multiple,
}
