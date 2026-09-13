namespace Bit.Butil;

/// <summary>
/// A per-method adjustment to the totals - a surcharge for one card network, a discount for one
/// wallet. The browser applies the modifier whose <see cref="SupportedMethods"/> matches the method
/// the user picked.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/PaymentRequest#modifiers">PaymentDetailsModifier</see>
/// </summary>
public class PaymentDetailsModifier
{
    /// <summary>The payment method identifier this modifier applies to.</summary>
    public required string SupportedMethods { get; set; }

    /// <summary>The total to use instead of <see cref="PaymentDetails.Total"/> for that method.</summary>
    public PaymentItem? Total { get; set; }

    /// <summary>Extra display lines added for that method - the surcharge itself, typically.</summary>
    public PaymentItem[]? AdditionalDisplayItems { get; set; }

    /// <summary>
    /// Method-specific data, serialized as it stands. See the note on <see cref="PaymentMethod.Data"/>
    /// about trimming.
    /// </summary>
    public object? Data { get; set; }
}
