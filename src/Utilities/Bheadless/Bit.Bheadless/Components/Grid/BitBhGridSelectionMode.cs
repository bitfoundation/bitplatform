namespace Bit.Bheadless;

/// <summary>Row selection behaviour of a <see cref="BitBhGrid"/>.</summary>
public enum BitBhGridSelectionMode
{
    /// <summary>Rows can't be selected.</summary>
    None,

    /// <summary>One selected row.</summary>
    Single,

    /// <summary>Many selected rows (Space toggles, Shift+Space / Shift+click extends, Ctrl+A selects all).</summary>
    Multiple,
}
