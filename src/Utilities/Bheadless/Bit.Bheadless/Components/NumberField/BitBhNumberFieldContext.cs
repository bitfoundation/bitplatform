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

/// <summary>
/// The state a <see cref="BitBhNumberField{TValue}"/> shares with its parts
/// (<see cref="BitBhNumberFieldInput"/>, <see cref="BitBhNumberFieldIncrement"/>,
/// <see cref="BitBhNumberFieldDecrement"/>, <see cref="BitBhNumberFieldScrubArea"/>). Not generic, so the
/// parts need no <c>TValue</c>.
/// </summary>
public abstract class BitBhNumberFieldContext
{
    internal BitBhNumberFieldContext() { }

    /// <summary>The id of the field's input.</summary>
    public abstract string InputId { get; }

    /// <summary>The current value (null when empty).</summary>
    public abstract decimal? NumericValue { get; }

    /// <summary>The text the input shows: what is being typed, or the formatted value.</summary>
    public abstract string DisplayText { get; }

    /// <summary>The formatted value (<c>aria-valuetext</c>), or null when empty.</summary>
    public abstract string? ValueText { get; }

    /// <summary>The minimum value, if any.</summary>
    public abstract decimal? MinValue { get; }

    /// <summary>The maximum value, if any.</summary>
    public abstract decimal? MaxValue { get; }

    /// <summary>Whether the field is disabled (own flag, field or fieldset).</summary>
    public abstract bool IsDisabled { get; }

    /// <summary>Whether the field is read-only.</summary>
    public abstract bool IsReadOnly { get; }

    /// <summary>Whether the field is invalid.</summary>
    public abstract bool IsInvalid { get; }

    /// <summary>Whether the field is required.</summary>
    public abstract bool IsRequired { get; }

    /// <summary>Whether the input has focus.</summary>
    public abstract bool IsFocused { get; }

    /// <summary>Whether a value is being scrubbed or repeated by a held button.</summary>
    public abstract bool IsScrubbing { get; }

    /// <summary>Whether the value can go up.</summary>
    public abstract bool CanIncrement { get; }

    /// <summary>Whether the value can go down.</summary>
    public abstract bool CanDecrement { get; }

    internal abstract BitBhFieldContext? Field { get; }

    internal abstract string InputMode { get; }

    internal abstract string AllowedCharacters { get; }

    internal abstract bool AllowWheelScrub { get; }

    internal abstract bool HasHomeEnd { get; }

    internal abstract double ScrubPixelSensitivity { get; }

    internal abstract void SetInputId(string? id);

    internal abstract Task OnTextInputAsync(string? text);

    internal abstract Task CommitTextAsync();

    internal abstract Task StepAsync(int direction, BitBhStepSize size, bool commit);

    internal abstract Task SetToBoundAsync(bool max);

    internal abstract Task CommitAsync();

    internal abstract void SetScrubbing(bool scrubbing);

    internal abstract void SetFocused(bool focused);
}
