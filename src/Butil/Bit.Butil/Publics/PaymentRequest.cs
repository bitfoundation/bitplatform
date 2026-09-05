using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Payment_Request_API">Payment Request API</see>
/// (<c>window.PaymentRequest</c>): the browser's own payment sheet, filled from the wallets and
/// cards the user already has.
/// </summary>
/// <remarks>
/// Secure context and a user gesture only, and the page has to be top-level or in an iframe
/// carrying <c>allow="payment"</c>. The flow is three calls, not one:
/// <see cref="Show"/> opens the sheet and returns what the user authorized, your server processes
/// <see cref="PaymentResponse.Details"/>, and <see cref="Complete"/> then dismisses the sheet with
/// the outcome. A sheet left uncompleted stays on screen.
/// <br/>
/// One sheet at a time: <see cref="Show"/> tracks the request it opened on this instance, which is
/// what <see cref="Abort"/> cancels.
/// </remarks>
[ButilService(typeof(PaymentRequest))]
public class PaymentRequest(IJSRuntime js)
{
    // The handle the JS side files this instance's in-flight request and its response under. Per
    // instance (the services are scoped), so one circuit's Abort cannot reach another's sheet - and
    // named the way the other cancellable services name theirs.
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    /// <summary>
    /// True when the runtime exposes <c>window.PaymentRequest</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest">PaymentRequest</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.paymentRequest.isSupported");

    /// <summary>
    /// Asks whether the browser could pay with any of <paramref name="methods"/> at all - a way to
    /// hide a pay button rather than have it open an empty sheet. No user gesture needed, and no UI
    /// is shown.
    /// </summary>
    /// <remarks>
    /// This is <c>canMakePayment()</c>, which answers about the <em>methods</em>, not about the
    /// user having a usable card behind them. Browsers rate-limit it.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/canMakePayment">PaymentRequest.canMakePayment()</see>
    /// </remarks>
    // PaymentDetails carries ShippingOptions and Modifiers, so serializing it reaches those two types
    // as well - a trimmed or AOT build that kept only the four obvious ones loses them here.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentMethod))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentDetails))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentDetailsModifier))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentItem))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentCurrencyAmount))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentShippingOption))]
    public ValueTask<bool> CanMakePayment(PaymentMethod[] methods, PaymentDetails details)
        => js.Invoke<bool>("BitButil.paymentRequest.canMakePayment", methods, details);

    /// <summary>
    /// Opens the payment sheet and resolves with what the user authorized, or <c>null</c> if they
    /// dismissed it or the browser refused. Must be called from a user-gesture handler.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="PaymentResponse"/> leaves the sheet open: process its
    /// <see cref="PaymentResponse.Details"/> server-side, then call <see cref="Complete"/> with the
    /// <see cref="PaymentResponse.Id"/> to dismiss it. A response left uncompleted is held until
    /// this instance's next <see cref="Show"/>, which replaces it.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/show">PaymentRequest.show()</see>
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentMethod))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentDetails))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentDetailsModifier))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentItem))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentCurrencyAmount))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentShippingOption))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentResponse))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentAddress))]
    public ValueTask<PaymentResponse?> Show(PaymentMethod[] methods, PaymentDetails details, PaymentOptions? options = null)
        => js.Invoke<PaymentResponse?>("BitButil.paymentRequest.show", _instanceId, methods, details, options);

    /// <summary>
    /// Dismisses the sheet a <see cref="Show"/> is still holding open, with the outcome of
    /// processing the payment.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentResponse/complete">PaymentResponse.complete()</see>
    /// </summary>
    /// <param name="responseId">The <see cref="PaymentResponse.Id"/> the sheet handed back.</param>
    /// <param name="result">What to tell the user. Defaults to <see cref="PaymentCompleteResult.Success"/>.</param>
    public ValueTask Complete(string responseId, PaymentCompleteResult result = PaymentCompleteResult.Success)
        => js.InvokeVoid("BitButil.paymentRequest.complete", responseId, ToName(result));

    /// <summary>
    /// Closes the sheet this instance opened, before the user has authorized anything - the cart
    /// expired, the item sold out. Returns false when there is nothing in flight to abort.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PaymentRequest/abort">PaymentRequest.abort()</see>
    /// </summary>
    public ValueTask<bool> Abort() => js.Invoke<bool>("BitButil.paymentRequest.abort", _instanceId);

    private static string ToName(PaymentCompleteResult result) => result switch
    {
        PaymentCompleteResult.Fail => "fail",
        PaymentCompleteResult.Unknown => "unknown",
        _ => "success"
    };
}
