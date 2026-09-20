using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Compute_Pressure_API">Compute Pressure API</see>
/// (<c>PressureObserver</c>): learn when the machine is under CPU or thermal pressure, so the app
/// can shed work before the user notices it stuttering.
/// </summary>
/// <remarks>
/// Chromium only, and only over HTTPS. Readings are a four-step scale rather than a number, on
/// purpose - a precise load figure would be a timing side channel.
/// <br/>
/// The obvious use is a video call dropping to a lower resolution, or a canvas app cutting its
/// effect quality, when the state reaches <c>"serious"</c>.
/// </remarks>
[ButilService(typeof(ComputePressure))]
public class ComputePressure(IJSRuntime js) : IAsyncDisposable
{
    internal const string RecordsMethodName = nameof(InvokePressureRecords);

    private readonly ConcurrentDictionary<Guid, Action<PressureRecord[]>> _handlers = new();

    // Per-instance callback reference (see Keyboard): observers are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<ComputePressure>? _dotNetRef;
    private DotNetObjectReference<ComputePressure> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>PressureObserver</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.computePressure.isSupported");

    /// <summary>
    /// The sources this browser can observe - <c>["cpu"]</c> today. Worth reading before
    /// hard-coding a source, since observing an unknown one simply fails.
    /// </summary>
    public ValueTask<string[]> GetKnownSources() => js.Invoke<string[]>("BitButil.computePressure.getKnownSources");

    /// <summary>
    /// Invoked from JS with each batch of pressure records. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(RecordsMethodName)]
    public void InvokePressureRecords(Guid id, PressureRecord[] records)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(records);
    }

    /// <summary>
    /// Starts observing a pressure source. The callback fires when the state changes, not on a
    /// timer, so a machine sitting at <c>"nominal"</c> costs nothing.
    /// </summary>
    /// <param name="handler">Called with the records since the last callback - usually one.</param>
    /// <param name="source">The source to watch. <c>"cpu"</c> is the only one shipping.</param>
    /// <param name="sampleIntervalMs">
    /// The shortest interval between samples, in milliseconds. 0 lets the browser choose, which is
    /// the right default - a short interval is throttled anyway.
    /// </param>
    /// <returns>A subscription - dispose it to stop observing.</returns>
    /// <exception cref="InvalidOperationException">
    /// The observer did not start - <c>PressureObserver</c> is missing, the source is unknown, or
    /// the observation was refused.
    /// </exception>
    [DynamicDependency(nameof(InvokePressureRecords), typeof(ComputePressure))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PressureRecord))]
    public async ValueTask<ButilSubscription> Observe(Action<PressureRecord[]> handler,
                                                      string source = "cpu",
                                                      int sampleIntervalMs = 0)
    {
        return await ButilSubscriptionHelper.Register(_handlers, handler,
                                                      id => js.InvokeRegister("BitButil.computePressure.observe", id, DotNetRef, source, sampleIntervalMs),
                                                      id => js.InvokeVoid("BitButil.computePressure.disconnect", id),
                                                      $"The '{source}' pressure source could not be observed.");
    }

    /// <summary>Disconnects every observer this instance started and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var id in _handlers.Keys.ToArray())
            {
                _handlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.computePressure.disconnect", id);
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }

        GC.SuppressFinalize(this);
    }
}
