namespace Bit.Butil;

/// <summary>
/// One shipping choice offered in the payment sheet. Only meaningful when
/// <see cref="PaymentOptions.RequestShipping"/> is set.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/PaymentRequest#shippingoptions">PaymentShippingOption</see>
/// </summary>
public class PaymentShippingOption
{
    /// <summary>The identifier handed back as <see cref="PaymentResponse.ShippingOption"/>.</summary>
    public required string Id { get; set; }

    /// <summary>The label shown to the user, e.g. <c>"Next-day delivery"</c>.</summary>
    public required string Label { get; set; }

    /// <summary>What this option costs.</summary>
    public required PaymentCurrencyAmount Amount { get; set; }

    /// <summary>
    /// True on the option that starts selected. At most one option may set it; setting none leaves
    /// the sheet with no shipping option chosen until the user picks one.
    /// </summary>
    public bool Selected { get; set; }
}
