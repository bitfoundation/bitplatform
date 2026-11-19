namespace Bit.Bheadless;

/// <summary>Which characters a <see cref="BitBhPinInput"/> accepts.</summary>
public enum BitBhPinInputType
{
    /// <summary>Digits only (0–9). Phones show a numeric keypad.</summary>
    Numeric,

    /// <summary>Letters and digits.</summary>
    Alphanumeric,

    /// <summary>Letters only.</summary>
    Alphabetic,
}
