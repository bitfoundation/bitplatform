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

    /// <summary>
    /// True when the runtime exposes <c>window.OTPCredential</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/OTPCredential">OTPCredential</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webOtp.isSupported");

    /// <summary>
    /// Waits for a matching SMS and returns the code from it, or <c>null</c> if the wait was
    /// aborted, timed out, or the user dismissed the browser's prompt.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">CredentialsContainer.get()</see>
    /// </summary>
    /// <param name="timeout">
    /// How long to wait before giving up. Unset waits indefinitely - which is why the input should
    /// stay usable: this is autofill, not a replacement for the user typing the code.
    /// <see cref="TimeSpan.Zero"/> gives up at once; only leaving it unset waits forever.
    /// </param>
    /// <param name="cancellationToken">Cancelling it aborts the wait, the same as <see cref="Abort"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="timeout"/> is negative.</exception>
    /// <remarks>
    /// Start the wait as the code-entry step appears, not on page load: the browser shows a prompt
    /// while one is pending, and a prompt with no visible reason is one users dismiss.
    /// <br/>
    /// During prerender/SSR (no JS runtime) there is no SMS to wait for and this returns <c>null</c>
    /// at once, indistinguishable from a dismissed prompt - another reason to start the wait from
    /// <c>OnAfterRenderAsync</c> or a click, not from <c>OnInitializedAsync</c>.
    /// </remarks>
    public async ValueTask<string?> Receive(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        // Rejected here rather than shrugged off in JS: a negative timeout is a caller's arithmetic
        // gone wrong, and silently reading it as "wait forever" leaves a prompt up with no deadline.
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be non-negative; leave it unset to wait indefinitely.");

        // A handle per call, not per instance: an already-cancelled token fires its abort before the
        // receive below is dispatched, and the JS side holds that abort against this handle alone -
        // so the wait it belongs to never starts, and the next Receive is left untouched.
        var requestId = Guid.NewGuid().ToString("N");

        // Registered rather than awaited against: the JS side owns an AbortController, and the only
        // way to end the browser's wait early is to trip it. Disposed with the call either way, so a
        // long-lived token doesn't accumulate registrations.
        using var registration = js.RegisterJsAbort(cancellationToken, "BitButil.webOtp.abort", _instanceId, requestId);

        var code = await js.Invoke<string?>("BitButil.webOtp.receive", _instanceId, requestId, timeout?.TotalMilliseconds);

        // The safe default for a string during prerender is "", and a code is never empty: "no code"
        // has one spelling here, the null the summary promises, so a caller's `is not null` check
        // doesn't submit an empty one.
        return string.IsNullOrEmpty(code) ? null : code;
    }

    /// <summary>
    /// Ends the wait started by <see cref="Receive"/> on this instance - the user chose to type the
    /// code, or moved on. Returns false when nothing was pending.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AbortController/abort">AbortController.abort()</see>
    /// </summary>
    public ValueTask<bool> Abort() => js.Invoke<bool>("BitButil.webOtp.abort", _instanceId, null);
}
