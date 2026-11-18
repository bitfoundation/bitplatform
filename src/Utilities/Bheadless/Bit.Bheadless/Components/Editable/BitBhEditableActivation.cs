namespace Bit.Bheadless;

/// <summary>What starts editing a <see cref="BitBhEditable"/> from its preview.</summary>
public enum BitBhEditableActivation
{
    /// <summary>Focusing the preview (Tab or click).</summary>
    Focus,

    /// <summary>Clicking the preview, or Enter / Space on it.</summary>
    Click,

    /// <summary>Double-clicking the preview, or Enter on it.</summary>
    DoubleClick,

    /// <summary>Only a <see cref="BitBhEditableEditTrigger"/> (or code) does.</summary>
    None,
}
