namespace Bit.Butil;

public enum InputMode
{
    /// <summary>
    /// The inputmode value is not set.
    /// </summary>
    NotSet,

    /// <summary>
    /// Fractional numeric input keyboard that contains the digits and decimal separator for the user's locale (typically . or ,).
    /// </summary>
    Decimal,

    /// <summary>
    /// A virtual keyboard optimized for entering email addresses. Typically includes the @character as well as other optimizations.
    /// </summary>
    Email,

    /// <summary>
    /// No virtual keyboard. This is used when the page implements its own keyboard input control.
    /// </summary>
    None,

    /// <summary>
    /// Numeric input keyboard that only requires the digits 0–9. Devices may or may not show a minus key.
    /// </summary>
    Numeric,

    /// <summary>
    /// A virtual keyboard optimized for search input. For instance, the return/submit key may be labeled "Search".
    /// </summary>
    Search,

    /// <summary>
    /// A telephone keypad input that includes the digits 0–9, the asterisk (*), and the pound (#) key.
    /// </summary>
    Tel,

    /// <summary>
    /// Standard input keyboard for the user's current locale.
    /// </summary>
    Text,

    /// <summary>
    /// A keypad optimized for entering URLs. This may have the / key more prominent, for example.
    /// </summary>
    Url,
}
