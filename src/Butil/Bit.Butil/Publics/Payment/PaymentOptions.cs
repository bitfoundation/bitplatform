namespace Bit.Butil;

/// <summary>
/// The contact details the payment sheet should collect alongside the payment itself. Each flag
/// adds a field the user has to fill in or confirm, so ask only for what you will use.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/PaymentRequest#options">PaymentOptions</see>
/// </summary>
public class PaymentOptions
{
    /// <summary>Collect the payer's name.</summary>
    public bool RequestPayerName { get; set; }

    /// <summary>Collect the payer's email address.</summary>
    public bool RequestPayerEmail { get; set; }

    /// <summary>Collect the payer's phone number.</summary>
    public bool RequestPayerPhone { get; set; }

    /// <summary>
    /// Collect a shipping address and let the user choose from
    /// <see cref="PaymentDetails.ShippingOptions"/>.
    /// </summary>
    public bool RequestShipping { get; set; }

    /// <summary>
    /// How the sheet labels the address it collects: <c>"shipping"</c> (the default),
    /// <c>"delivery"</c> or <c>"pickup"</c>.
    /// </summary>
    public string? ShippingType { get; set; }
}
