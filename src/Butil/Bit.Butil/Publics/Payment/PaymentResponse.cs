using System.Text.Json;

namespace Bit.Butil;

/// <summary>
/// What the user authorized. The payment is <em>not</em> finished when this arrives: the sheet is
/// still open, waiting for <see cref="PaymentRequest.Complete"/> to be called with the outcome of
/// processing <see cref="Details"/> on your server.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentResponse">PaymentResponse</see>
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// The handle this response is tracked under on the JavaScript side - pass it to
    /// <see cref="PaymentRequest.Complete"/>. It is Butil's own identifier, not the browser's, and
    /// it belongs to the <see cref="PaymentRequest"/> instance that opened the sheet: the next
    /// <see cref="PaymentRequest.Show"/> on that instance replaces the response held under it.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The payment's identifier: <see cref="PaymentDetails.Id"/> if you set one, otherwise the one
    /// the browser generated.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>The payment method identifier the user paid with.</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// The method-specific payload - the part a payment processor consumes. Its shape is defined by
    /// the payment method, so it arrives as raw JSON rather than a typed object. Send it to the
    /// server as it stands; never trust a client-side reading of it.
    /// </summary>
    public JsonElement Details { get; set; }

    /// <summary>The payer's name, if <see cref="PaymentOptions.RequestPayerName"/> was set.</summary>
    public string? PayerName { get; set; }

    /// <summary>The payer's email, if <see cref="PaymentOptions.RequestPayerEmail"/> was set.</summary>
    public string? PayerEmail { get; set; }

    /// <summary>The payer's phone, if <see cref="PaymentOptions.RequestPayerPhone"/> was set.</summary>
    public string? PayerPhone { get; set; }

    /// <summary>The <see cref="PaymentShippingOption.Id"/> the user chose, when shipping was requested.</summary>
    public string? ShippingOption { get; set; }

    /// <summary>The address the user chose, when shipping was requested.</summary>
    public PaymentAddress? ShippingAddress { get; set; }
}
