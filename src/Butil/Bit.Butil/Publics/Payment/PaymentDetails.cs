namespace Bit.Butil;

/// <summary>
/// What is being charged: the total, the lines behind it, and - when shipping was requested - the
/// shipping options to choose from.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/PaymentRequest#details">PaymentDetailsInit</see>
/// </summary>
public class PaymentDetails
{
    /// <summary>
    /// A free-form identifier for this payment, echoed back as <see cref="PaymentResponse.RequestId"/>.
    /// Left unset, the browser generates one.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>The amount being charged. This is the number the sheet leads with.</summary>
    public required PaymentItem Total { get; set; }

    /// <summary>
    /// The lines making up the total. The browser neither adds them up nor checks them against
    /// <see cref="Total"/> - they are display only, and keeping them honest is on you.
    /// </summary>
    public PaymentItem[]? DisplayItems { get; set; }

    /// <summary>
    /// The shipping choices to offer. Ignored unless <see cref="PaymentOptions.RequestShipping"/>
    /// is set.
    /// </summary>
    public PaymentShippingOption[]? ShippingOptions { get; set; }

    /// <summary>Per-method adjustments to the totals.</summary>
    public PaymentDetailsModifier[]? Modifiers { get; set; }
}
