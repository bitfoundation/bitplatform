namespace Bit.Butil;

/// <summary>Who decides when the on-screen keyboard appears for a <c>contenteditable</c> element.</summary>
public enum VirtualKeyboardPolicy
{
    /// <summary>The attribute is absent - the browser behaves as if it were <see cref="Auto"/>.</summary>
    NotSet,

    /// <summary>The browser shows and hides the keyboard as focus moves. The default.</summary>
    Auto,

    /// <summary>The page controls it through the VirtualKeyboard API instead.</summary>
    Manual
}
