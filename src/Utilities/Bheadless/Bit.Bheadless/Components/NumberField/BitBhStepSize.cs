namespace Bit.Bheadless;

/// <summary>How far a number field steps.</summary>
public enum BitBhStepSize
{
    /// <summary>One <c>Step</c> (arrow keys, the increment / decrement buttons).</summary>
    Normal,

    /// <summary>One <c>LargeStep</c> (Shift + arrows, PageUp / PageDown).</summary>
    Large,

    /// <summary>One <c>SmallStep</c> (Alt + arrows).</summary>
    Small,
}
