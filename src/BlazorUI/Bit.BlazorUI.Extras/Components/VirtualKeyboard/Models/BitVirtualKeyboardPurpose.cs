using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// What is being typed with a <see cref="BitVirtualKeyboard"/>, which picks its layout.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardPurpose>))]
public enum BitVirtualKeyboardPurpose
{
    /// <summary>Text: the language layout.</summary>
    Text,

    /// <summary>A search: the language layout, with a Search key.</summary>
    Search,

    /// <summary>An email address: the language layout with @ and .com keys.</summary>
    Email,

    /// <summary>A URL: the language layout with / and .com keys.</summary>
    Url,

    /// <summary>A telephone number: the phone pad.</summary>
    Tel,

    /// <summary>Digits: the numeric pad.</summary>
    Numeric,

    /// <summary>A decimal number: the decimal pad.</summary>
    Decimal,

    /// <summary>A signed decimal number: the decimal pad with a minus key.</summary>
    SignedDecimal,

    /// <summary>A PIN: the PIN pad.</summary>
    Pin,

    /// <summary>A calculation: the calculator pad.</summary>
    Calculator,

    /// <summary>An amount at a point of sale: the POS pad.</summary>
    Pos,

    /// <summary>A hexadecimal number: the hex pad.</summary>
    Hex,

    /// <summary>A date: the date pad.</summary>
    Date,

    /// <summary>A time: the time pad.</summary>
    Time,

    /// <summary>An IBAN: digits and capital letters.</summary>
    Iban,

    /// <summary>Letters in alphabetical order: the kiosk grid.</summary>
    Alphabetic,

    /// <summary>An ATM-style PIN: the ATM pad.</summary>
    Atm
}