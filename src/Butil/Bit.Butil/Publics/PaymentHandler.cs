using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the page-side half of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Payment_Handler_API">Payment Handler API</see>
/// (<c>ServiceWorkerRegistration.paymentManager</c>) - what an installed web app registers so that
/// <em>other</em> sites' <see cref="PaymentRequest"/> sheets can offer it as a payment method.
/// </summary>
/// <remarks>
/// This is the payer's side of the boundary, not the merchant's. The handling itself happens in the
/// service worker, which answers the <c>canmakepayment</c> and <c>paymentrequest</c> events - those
/// live in worker script and are out of reach of a Blazor component. What a page can do is what is
/// wrapped here: name the account the browser shows next to your app, and declare which parts of
/// the sheet your handler collects itself.
/// <br/>
/// Chromium only, secure context only, and every member needs an active service worker
/// registration - each call awaits <c>navigator.serviceWorker.ready</c>.
/// </remarks>
[ButilService(typeof(PaymentHandler))]
public class PaymentHandler(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>ServiceWorkerRegistration.paymentManager</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.paymentHandler.isSupported");

    /// <summary>
    /// The hint shown beneath your app's name in another site's payment sheet - usually the signed-in
    /// account, e.g. <c>"user@example.com"</c>. Empty when none is set.
    /// </summary>
    public ValueTask<string> GetUserHint() => js.Invoke<string>("BitButil.paymentHandler.getUserHint");

    /// <summary>
    /// Sets the account hint. Set it after sign-in and clear it on sign-out: it is shown to users of
    /// other sites, so it should never outlive the session it describes.
    /// </summary>
    public ValueTask SetUserHint(string userHint) => js.InvokeVoid("BitButil.paymentHandler.setUserHint", userHint);

    /// <summary>
    /// Declares which fields your handler will collect itself, so the browser stops asking for them:
    /// <c>"shippingAddress"</c>, <c>"payerName"</c>, <c>"payerEmail"</c>, <c>"payerPhone"</c>.
    /// Returns the delegations the browser accepted.
    /// </summary>
    /// <remarks>
    /// Delegating a field makes your service worker responsible for putting it in the response;
    /// omitting it there leaves the merchant with a blank where the user filled something in.
    /// </remarks>
    public ValueTask<string[]> EnableDelegations(string[] delegations)
        => js.Invoke<string[]>("BitButil.paymentHandler.enableDelegations", delegations);
}
