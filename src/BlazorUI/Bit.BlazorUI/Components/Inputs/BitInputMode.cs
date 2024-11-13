namespace Bit.BlazorUI;

/// <summary>
/// This allows a browser to display an appropriate virtual keyboard.
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/inputmode"/>
/// </summary>
public enum BitInputMode
{
    /// <summary>
    /// No virtual keyboard. For when the page implements its own keyboard input control.
    /// </summary>
    None,

    /// <summary>
    /// Standard input keyboard for the user's current locale.
    /// </summary>
    Text,

    /// <summary>
    /// Fractional numeric input keyboard containing the digits and decimal separator for the user's locale.
    /// </summary>
    Decimal,

    /// <summary>
    /// Numeric input keyboard, but only requires the digits 0–9.
    /// </summary>
    Numeric,

    /// <summary>
    /// A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key
    /// </summary>
    Tel,

    /// <summary>
    /// A virtual keyboard optimized for search input.
    /// </summary>
    Search,

    /// <summary>
    /// A virtual keyboard optimized for entering email addresses.
    /// </summary>
    Email,

    /// <summary>
    /// A keypad optimized for entering URLs.
    /// </summary>
    Url
}
