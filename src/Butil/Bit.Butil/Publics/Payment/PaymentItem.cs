namespace Bit.Butil;

/// <summary>
/// One line in the payment sheet - the total itself, or a display item beneath it.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/PaymentRequest#displayitems">PaymentItem</see>
/// </summary>
public class PaymentItem
{
    /// <summary>The human-readable label the browser shows for this line.</summary>
    public required string Label { get; set; }

    /// <summary>The amount of this line.</summary>
    public required PaymentCurrencyAmount Amount { get; set; }

    /// <summary>
    /// True when the amount is not final yet - shipping still unknown, say. The browser renders
    /// pending lines differently so a guess is not shown to the user as a fact.
    /// </summary>
    public bool Pending { get; set; }
}
