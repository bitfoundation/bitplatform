using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitVirtualKeyboard"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the virtual keyboards of an app agree on: their mode, form, language, timing, feedback,
/// size, look and words. What one keyboard types into and what it holds (Value, Open, Target, TargetSelector,
/// FollowFocus, Purpose, Layout, LayoutSelector, KeyTemplate, Scramble, MaxLength) and its event callbacks are the state
/// of a single keyboard.
/// </remarks>
public partial class BitVirtualKeyboardParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitVirtualKeyboard"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitVirtualKeyboard value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitVirtualKeyboard";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitVirtualKeyboardParams))]
    public BitVirtualKeyboardParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The character-class body of the characters the keyboards may type.
    /// </summary>
    public string? AllowedCharacters { get; set; }

    /// <summary>
    /// Whether a layout without a shift layer gets one derived from its letters.
    /// </summary>
    public bool? AutoShift { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the virtual keyboard.
    /// </summary>
    public BitVirtualKeyboardClassStyles? Classes { get; set; }

    /// <summary>
    /// The width under which an Auto form renders the Compact layout.
    /// </summary>
    public double? CompactBreakpoint { get; set; }

    /// <summary>
    /// The culture of the separators, the case mapping and the default language.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The digits the purpose pads type.
    /// </summary>
    public BitVirtualKeyboardDigits? Digits { get; set; }

    /// <summary>
    /// What the Enter key does.
    /// </summary>
    public BitVirtualKeyboardEnterBehavior? EnterBehavior { get; set; }

    /// <summary>
    /// The form of the language layouts.
    /// </summary>
    public BitVirtualKeyboardForm? Form { get; set; }

    /// <summary>
    /// Whether a key press vibrates, where the device can.
    /// </summary>
    public bool? Haptics { get; set; }

    /// <summary>
    /// The id of the language layout.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The ids of the layouts the language key cycles through.
    /// </summary>
    public IReadOnlyList<string>? Languages { get; set; }

    /// <summary>
    /// The milliseconds a key is held before its alternates open.
    /// </summary>
    public int? LongPressDelay { get; set; }

    /// <summary>
    /// Where the keyboard is shown.
    /// </summary>
    public BitVirtualKeyboardMode? Mode { get; set; }

    /// <summary>
    /// The milliseconds within which a repeated tap cycles a multitap key.
    /// </summary>
    public int? MultiTapTimeout { get; set; }

    /// <summary>
    /// The milliseconds before a held key starts repeating.
    /// </summary>
    public int? RepeatDelay { get; set; }

    /// <summary>
    /// The milliseconds between the repeats of a held key.
    /// </summary>
    public int? RepeatInterval { get; set; }

    /// <summary>
    /// Whether a docked keyboard keeps its height in the page.
    /// </summary>
    public bool? ReserveSpace { get; set; }

    /// <summary>
    /// Whether the keyboards are in secure mode.
    /// </summary>
    public bool? Secure { get; set; }

    /// <summary>
    /// Whether a press shows the enlarged-key bubble.
    /// </summary>
    public bool? ShowKeyPreview { get; set; }

    /// <summary>
    /// The size of the keys.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Whether a key press clicks.
    /// </summary>
    public bool? Sound { get; set; }

    /// <summary>
    /// The words of the keyboard.
    /// </summary>
    public BitVirtualKeyboardStrings? Strings { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the virtual keyboard.
    /// </summary>
    public BitVirtualKeyboardClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitVirtualKeyboard"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitVirtualKeyboard"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitVirtualKeyboard"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitVirtualKeyboard"/>.
    /// </remarks>
    /// <param name="bitVirtualKeyboard">
    /// The <see cref="BitVirtualKeyboard"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitVirtualKeyboard bitVirtualKeyboard)
    {
        if (bitVirtualKeyboard is null) return;

        UpdateBaseParameters(bitVirtualKeyboard);

        if (AllowedCharacters is not null)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(AllowedCharacters), AllowedCharacters, static c => c.AllowedCharacters, static (c, v) => c.AllowedCharacters = v);
        }

        if (AutoShift.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(AutoShift), AutoShift.Value, static c => c.AutoShift, static (c, v) => c.AutoShift = v);
        }

        if (Classes is not null)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (CompactBreakpoint.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(CompactBreakpoint), CompactBreakpoint.Value, static c => c.CompactBreakpoint, static (c, v) => c.CompactBreakpoint = v);
        }

        if (Culture is not null)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Culture), Culture, static c => c.Culture, static (c, v) => c.Culture = v);
        }

        if (Digits.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Digits), Digits.Value, static c => c.Digits, static (c, v) => c.Digits = v);
        }

        if (EnterBehavior.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(EnterBehavior), EnterBehavior.Value, static c => c.EnterBehavior, static (c, v) => c.EnterBehavior = v);
        }

        if (Form.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Form), Form.Value, static c => c.Form, static (c, v) => c.Form = v);
        }

        if (Haptics.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Haptics), Haptics.Value, static c => c.Haptics, static (c, v) => c.Haptics = v);
        }

        if (Language is not null)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Language), Language, static c => c.Language, static (c, v) => c.Language = v);
        }

        if (Languages is not null)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Languages), Languages, static c => c.Languages, static (c, v) => c.Languages = v);
        }

        if (LongPressDelay.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(LongPressDelay), LongPressDelay.Value, static c => c.LongPressDelay, static (c, v) => c.LongPressDelay = v);
        }

        if (Mode.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Mode), Mode.Value, static c => c.Mode, static (c, v) => c.Mode = v);
        }

        if (MultiTapTimeout.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(MultiTapTimeout), MultiTapTimeout.Value, static c => c.MultiTapTimeout, static (c, v) => c.MultiTapTimeout = v);
        }

        if (RepeatDelay.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(RepeatDelay), RepeatDelay.Value, static c => c.RepeatDelay, static (c, v) => c.RepeatDelay = v);
        }

        if (RepeatInterval.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(RepeatInterval), RepeatInterval.Value, static c => c.RepeatInterval, static (c, v) => c.RepeatInterval = v);
        }

        if (ReserveSpace.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(ReserveSpace), ReserveSpace.Value, static c => c.ReserveSpace, static (c, v) => c.ReserveSpace = v);
        }

        if (Secure.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Secure), Secure.Value, static c => c.Secure, static (c, v) => c.Secure = v);
        }

        if (ShowKeyPreview.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(ShowKeyPreview), ShowKeyPreview.Value, static c => c.ShowKeyPreview, static (c, v) => c.ShowKeyPreview = v);
        }

        if (Size.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (Sound.HasValue)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Sound), Sound.Value, static c => c.Sound, static (c, v) => c.Sound = v);
        }

        if (Strings is not null)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Strings), Strings, static c => c.Strings, static (c, v) => c.Strings = v);
        }

        if (Styles is not null)
        {
            bitVirtualKeyboard.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}