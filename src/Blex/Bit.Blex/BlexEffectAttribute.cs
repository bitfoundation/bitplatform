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
}
