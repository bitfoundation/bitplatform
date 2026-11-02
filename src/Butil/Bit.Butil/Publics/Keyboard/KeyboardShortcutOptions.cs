namespace Bit.Butil;

/// <summary>
/// How a <see cref="Keyboard"/> shortcut matches its key, and what it does to the key besides calling its handler.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/keydown_event">Element: keydown event</see>
/// </summary>
public sealed class KeyboardShortcutOptions
{
    /// <summary>The modifiers that must be down. Matched exactly, so extra modifiers do not fire it.</summary>
    public ButilModifiers Modifiers { get; set; }

    /// <summary>Suppress the browser's own action for the combination. <see langword="true"/> by default.</summary>
    public bool PreventDefault { get; set; } = true;

    /// <summary>Stop the keydown travelling any further. <see langword="true"/> by default.</summary>
    public bool StopPropagation { get; set; } = true;

    /// <summary>Keep firing while the key is held, instead of once per press.</summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// Whether the shortcut stands aside - neither firing nor suppressing anything - when the key goes to a control
    /// that acts on that key itself, so the key does there what it always does:
    /// <list type="bullet">
    /// <item>a text field - an <c>input</c> that takes text, a <c>textarea</c>, <c>contenteditable</c> content, or an
    /// element driven through <see cref="TextEditContext"/> - for the keys that type, delete or move the caret, and
    /// Enter where it adds a line or submits a form. A read-only field acts on none of them;</item>
    /// <item>a <c>select</c> for the arrows, Home, End, PageUp, PageDown, Space, Enter and the letters and digits;</item>
    /// <item>a range input for the arrows, Home, End, PageUp and PageDown, a radio button for the arrows and Space,
    /// a checkbox for Space, and a color or file input for Space and Enter;</item>
    /// <item>an element declaring one of those controls by its <c>role</c>, and a form-associated custom element.</item>
    /// </list>
    /// <see langword="null"/>, the default, stands aside only for a shortcut without Ctrl, Alt or Meta: those are the
    /// ones whose key a control types or acts on, while Ctrl+S and its kind replace the browser's own action wherever
    /// focus is. <see langword="true"/> stands aside whatever the modifiers, <see langword="false"/> never does.
    /// <br/>
    /// A shortcut scoped to an element with <see cref="Keyboard.SubscribeOn(Microsoft.AspNetCore.Components.ElementReference, string, System.Action, KeyboardShortcutOptions)"/>
    /// never stands aside for that element itself - scoping one to a text field means it is meant to fire there - only
    /// for the controls inside it.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/isContentEditable">HTMLElement.isContentEditable</see>
    /// </summary>
    /// <remarks>
    /// A key typed inside a closed shadow root reaches the shortcut with the shadow host as its target, and nothing
    /// outside that root can see what has focus inside it. Such a host is read by what it declares - its <c>role</c>,
    /// or being form-associated, which counts as a text field - and a host declaring neither counts as no control.
    /// </remarks>
    public bool? IgnoreWhenEditable { get; set; }
}
