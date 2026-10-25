namespace Bit.Blex;

/// <summary>
/// Marks an asynchronous method (returning <c>Task</c> or <c>ValueTask</c>, named <c>OnXxx</c>) as an
/// effect: an async action whose loading/error lifecycle is managed for you. The generator emits the
/// public <c>Xxx(...)</c> wrapper plus reactive <c>XxxIsLoading</c> (bool) and <c>XxxError</c>
/// (<see cref="Exception"/>) properties. The wrapper keeps <c>IsLoading</c> <c>true</c> while any
/// run is in flight, captures any thrown exception into <c>Error</c> instead of propagating it
/// (cancellations are not treated as errors), and records the body as one action.
/// </summary>
/// <remarks>
/// When the effect method's last parameter is a <see cref="System.Threading.CancellationToken"/>,
/// the token is provided by the generated wrapper (it does not appear on the public method) and a
/// <c>CancelXxx()</c> method is emitted to cancel in-flight runs.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class BlexEffectAttribute : Attribute
{
    /// <summary>
    /// Optional explicit name. A valid C# identifier becomes the wrapper method name and display
    /// label; a value with spaces is treated as the display label only (wrapper derived from <c>On</c>).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>How overlapping invocations of this effect are handled. Defaults to <see cref="BlexEffectConcurrency.Parallel"/>.</summary>
    public BlexEffectConcurrency Concurrency { get; set; }

    /// <summary>
    /// Waits for this quiet period (in milliseconds) before running the effect body, restarting the
    /// wait on every new invocation -- the search-as-you-type gate. <c>0</c> (the default) disables
    /// debouncing. Only the final invocation of a burst runs; the ones it superseded return without
    /// having touched anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The wait happens before <em>any</em> observable side effect, so a superseded invocation
    /// never flips <c>XxxIsLoading</c>, never clears <c>XxxError</c>, is never offered to middleware
    /// and records no action. <c>XxxIsLoading</c> therefore turns <c>true</c> when the body actually
    /// starts, not while the gate is waiting -- bind a spinner to it and it will not flicker once
    /// per keystroke.
    /// </para>
    /// <para>
    /// Debouncing composes with <see cref="Concurrency"/>, which still governs invocations that
    /// survive the gate: pair it with <see cref="BlexEffectConcurrency.Latest"/> (and a trailing
    /// <see cref="System.Threading.CancellationToken"/> parameter) so a request already in flight is
    /// also cancelled when the next one gets through.
    /// </para>
    /// </remarks>
    /// <example><code>
    /// [BlexEffectAttribute(DebounceMilliseconds = 300, Concurrency = BlexEffectConcurrency.Latest)]
    /// private async Task OnSearch(string query, CancellationToken ct) => Results = await _api.SearchAsync(query, ct);
    /// </code></example>
    public int DebounceMilliseconds { get; set; }

    /// <summary>
    /// How many times a failed run is retried before its exception lands in <c>XxxError</c>.
    /// <c>0</c> (the default) runs the body once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every failure is retried except a cancellation through the effect's own token
    /// (<c>CancelXxx()</c>, a superseding <see cref="BlexEffectConcurrency.Latest"/> run, the store being
    /// disposed), which ends the run at once. A <see cref="TimeoutMilliseconds">timed-out</see> attempt is
    /// a failure like any other. The attempts are one run: <c>XxxIsLoading</c> stays <c>true</c> across
    /// them, the action is recorded once, and only the final failure reaches <c>XxxError</c> and the
    /// action-error hooks.
    /// </para>
    /// <para>
    /// The body runs again from the top, so keep what it changes before its last <c>await</c> safe to
    /// repeat. Waits use the manager's <see cref="System.TimeProvider"/>, so a test advances a fake clock
    /// instead of waiting.
    /// </para>
    /// </remarks>
    /// <example><code>
    /// [BlexEffectAttribute(RetryCount = 3, TimeoutMilliseconds = 10_000)]
    /// private async Task OnLoad(CancellationToken ct) => Orders = await _api.GetOrdersAsync(ct);
    /// </code></example>
    public int RetryCount { get; set; }

    /// <summary>
    /// The wait before the first retry, in milliseconds; it doubles before each further retry, up to 30
    /// seconds (exponential backoff). Defaults to <c>1000</c>; <c>0</c> retries at once. Only meaningful
    /// together with <see cref="RetryCount"/>.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// How long one attempt may take, in milliseconds, before its token is cancelled and it fails with a
    /// <see cref="TimeoutException"/>. <c>0</c> (the default) sets no limit. Requires a trailing
    /// <see cref="System.Threading.CancellationToken"/> parameter -- the token is how the body is told to
    /// stop (<c>BLEX023</c>).
    /// </summary>
    public int TimeoutMilliseconds { get; set; }
}
