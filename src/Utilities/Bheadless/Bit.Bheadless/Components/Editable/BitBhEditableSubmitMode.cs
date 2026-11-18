namespace Bit.Bheadless;

/// <summary>What commits an edit of a <see cref="BitBhEditable"/> (Escape always cancels).</summary>
public enum BitBhEditableSubmitMode
{
    /// <summary>Enter, or leaving the input.</summary>
    Both,

    /// <summary>Enter only (leaving the input cancels).</summary>
    Enter,

    /// <summary>Leaving the input only (Enter does nothing).</summary>
    Blur,

    /// <summary>Only a <see cref="BitBhEditableSubmitTrigger"/> (or code) does.</summary>
    None,
}
