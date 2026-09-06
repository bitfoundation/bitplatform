namespace Bit.Butil;

/// <summary>
/// A monetary amount: a currency code and the value as a decimal <em>string</em>.
/// <br/>
/// The value is a string on purpose - the browser API takes one, and binary floating point cannot
/// hold "19.99" exactly. Format it yourself (<c>amount.ToString("0.00", CultureInfo.InvariantCulture)</c>)
/// so a comma-decimal culture can't produce "19,99".
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/PaymentRequest#total">PaymentCurrencyAmount</see>
/// </summary>
public class PaymentCurrencyAmount
{
    /// <summary>The ISO 4217 currency code, e.g. <c>"USD"</c> or <c>"EUR"</c>.</summary>
    public required string Currency { get; set; }

    /// <summary>The amount as a decimal string with a <c>.</c> separator, e.g. <c>"19.99"</c>.</summary>
    public required string Value { get; set; }
}
