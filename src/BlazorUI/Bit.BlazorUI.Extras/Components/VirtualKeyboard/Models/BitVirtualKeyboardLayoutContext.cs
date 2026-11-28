using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// What a <see cref="BitVirtualKeyboard.LayoutSelector"/> is told to pick a layout by.
/// </summary>
public sealed class BitVirtualKeyboardLayoutContext
{
    /// <summary>
    /// The purpose of what is being typed.
    /// </summary>
    public BitVirtualKeyboardPurpose Purpose { get; init; }

    /// <summary>
    /// The type attribute of the input typed into, if any.
    /// </summary>
    public string? InputType { get; init; }

    /// <summary>
    /// The inputmode attribute the input typed into had before the keyboard attached to it, if any.
    /// </summary>
    public string? InputMode { get; init; }

    /// <summary>
    /// The enterkeyhint attribute of the input typed into, if any.
    /// </summary>
    public string? EnterKeyHint { get; init; }

    /// <summary>
    /// The id of the input typed into, if any.
    /// </summary>
    public string? TargetId { get; init; }

    /// <summary>
    /// The culture of the keyboard.
    /// </summary>
    public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;

    /// <summary>
    /// The form to show (Full or Compact; Auto is already resolved).
    /// </summary>
    public BitVirtualKeyboardForm Form { get; init; }
}