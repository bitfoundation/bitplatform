namespace Bit.Butil;

/// <summary>
/// Wire shape of a shortcut handed to <c>BitButil.keyboard.add</c>/<c>addOn</c>: one object instead of a run of
/// positional booleans that two neighbours could swap unnoticed. The modifiers travel as separate flags and
/// <see cref="IgnoreWhenEditable"/> is already resolved from its default, so the script only compares.
/// </summary>
internal sealed class JsKeyboardShortcut
{
    public string Code { get; set; } = string.Empty;
    public bool Alt { get; set; }
    public bool Ctrl { get; set; }
    public bool Meta { get; set; }
    public bool Shift { get; set; }
    public bool PreventDefault { get; set; }
    public bool StopPropagation { get; set; }
    public bool Repeat { get; set; }
    public bool IgnoreWhenEditable { get; set; }

    public static JsKeyboardShortcut From(string code, KeyboardShortcutOptions options) => new()
    {
        Code = code,
        Alt = options.Modifiers.HasFlag(ButilModifiers.Alt),
        Ctrl = options.Modifiers.HasFlag(ButilModifiers.Ctrl),
        Meta = options.Modifiers.HasFlag(ButilModifiers.Meta),
        Shift = options.Modifiers.HasFlag(ButilModifiers.Shift),
        PreventDefault = options.PreventDefault,
        StopPropagation = options.StopPropagation,
        Repeat = options.Repeat,
        IgnoreWhenEditable = options.IgnoreWhenEditable
            ?? (options.Modifiers & (ButilModifiers.Ctrl | ButilModifiers.Alt | ButilModifiers.Meta)) == ButilModifiers.None
    };
}
