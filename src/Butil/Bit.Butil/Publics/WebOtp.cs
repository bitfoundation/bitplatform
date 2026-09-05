using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebOTP_API">WebOTP API</see>
/// (<c>navigator.credentials.get({ otp })</c>): reads the one-time code out of an incoming SMS so the
/// user never has to switch apps to copy it.
/// </summary>
/// <remarks>
/// Chromium on Android only, secure context only. The message itself has to opt in - its last line
/// must name the origin and carry the code:
/// <code>
/// Your code is 123456
///
/// @example.com #123456
/// </code>
/// The origin after <c>@</c> has to be exactly the page's, and the code after <c>#</c> is what
/// <see cref="Receive"/> returns. Nothing else in the message is read, and a message for another
/// origin is never offered.
/// <br/>
/// One receive at a time per instance: <see cref="Abort"/> cancels the one in flight, and so does
/// cancelling the token passed to <see cref="Receive"/>.
/// </remarks>
[ButilService(typeof(WebOtp))]
public class WebOtp(IJSRuntime js)
{
    // Per-instance handle for the pending requests, so one circuit's Abort cannot cancel another's.
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    /// <summary>True when the runtime exposes <c>window.OTPCredential</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webOtp.isSupported");

    /// <summary>
    /// Waits for a matching SMS and returns the code from it, or <c>null</c> if the wait was
    /// aborted, timed out, or the user dismissed the browser's prompt.
    /// </summary>
    /// <param name="timeout">
    /// How long to wait before giving up. Unset waits indefinitely - which is why the input should
    /// stay usable: this is autofill, not a replacement for the user typing the code.
    /// </param>
    /// <param name="cancellationToken">Cancelling it aborts the wait, the same as <see cref="Abort"/>.</param>
    /// <remarks>
    /// Start the wait as the code-entry step appears, not on page load: the browser shows a prompt
    /// while one is pending, and a prompt with no visible reason is one users dismiss.
    /// </remarks>
    public async ValueTask<string?> Receive(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        // A handle per call, not per instance: an already-cancelled token fires its abort before the
        // receive below is dispatched, and the JS side holds that abort against this handle alone -
        // so the wait it belongs to never starts, and the next Receive is left untouched.
        var requestId = Guid.NewGuid().ToString("N");

        // Registered rather than awaited against: the JS side owns an AbortController, and the only
        // way to end the browser's wait early is to trip it. Disposed with the call either way, so a
        // long-lived token doesn't accumulate registrations.
        using var registration = cancellationToken.Register(() => js.InvokeVoid("BitButil.webOtp.abort", _instanceId, requestId));

        return await js.Invoke<string?>("BitButil.webOtp.receive", _instanceId, requestId, timeout?.TotalMilliseconds);
    }

    /// <summary>
    /// Ends the wait started by <see cref="Receive"/> on this instance - the user chose to type the
    /// code, or moved on. Returns false when nothing was pending.
    /// </summary>
    public ValueTask<bool> Abort() => js.Invoke<bool>("BitButil.webOtp.abort", _instanceId, null);
}
