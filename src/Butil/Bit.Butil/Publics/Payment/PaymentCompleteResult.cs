namespace Bit.Butil;

/// <summary>
/// How a payment ended, as told to <see cref="PaymentRequest.Complete"/>. This is what dismisses
/// the sheet, so it is sent once the server has actually answered.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentResponse/complete">PaymentResponse.complete()</see>
/// </summary>
public enum PaymentCompleteResult
{
    /// <summary>The payment was processed successfully.</summary>
    Success,

    /// <summary>The payment failed - the sheet reports the failure to the user.</summary>
    Fail,

    /// <summary>The outcome is not known yet; the sheet closes without stating either way.</summary>
    Unknown,
}
