using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion;

/// <summary>
/// Carries "this page was prerendered with elements at rest" from the prerendering pass to the
/// interactive runtime that replaces it - one per DI scope.
/// <para>
/// Blazor doesn't hydrate prerendered markup: the interactive renderer throws it away and renders
/// every component again. An element prerendered at rest (<see cref="BmPrerenderMode.Final"/> /
/// <see cref="BmPrerenderMode.Animate"/>) would therefore jump back to <c>Initial</c> and play its
/// enter animation a second time. To prevent that, the prerendering pass persists a marker through
/// <see cref="PersistentComponentState"/>, and elements mounting in the runtime's first render read
/// it and render straight into their resting state.
/// </para>
/// </summary>
internal sealed class BmotionPrerenderState : IDisposable
{
    internal const string StateKey = "Bit.Bmotion.Prerendered";

    private readonly PersistentComponentState? _state;
    private readonly IBmotionInterop _interop;
    private PersistingComponentStateSubscription _subscription;
    private bool _marked;
    private bool? _hydrating;
    private bool _elapsedRead;
    private double? _elapsedMs;

    public BmotionPrerenderState(IServiceProvider services, IBmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(interop);
        // Optional: hosts that never prerender (e.g. a BlazorWebView) may not register it at all.
        _state = services.GetService<PersistentComponentState>();
        _interop = interop;
    }

    /// <summary>
    /// Prerendering pass: arranges for the marker to be persisted with the page. The first element
    /// to call this registers <paramref name="persist"/>, which must be a method of the element
    /// itself - Blazor infers which interactive runtime the state is for from the component that
    /// registered the callback.
    /// </summary>
    internal void MarkPrerendered(Func<Task> persist)
    {
        if (_marked || _state is null) return;
        _marked = true;
        try
        {
            _subscription = _state.RegisterOnPersisting(persist);
        }
        catch (InvalidOperationException)
        {
            // The state has already been persisted (a component rendered after the response's
            // state was written); the runtime will animate as if nothing was prerendered.
        }
    }

    /// <summary>Writes the marker. Invoked through the callback registered in <see cref="MarkPrerendered"/>.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Persists a bool, which System.Text.Json serializes without reflection over user types.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Persists a bool, which System.Text.Json serializes without runtime code generation.")]
    internal Task PersistMarkerAsync()
    {
        try
        {
            _state?.PersistAsJson(StateKey, true);
        }
        catch (ArgumentException)
        {
            // Already persisted for this store: every store gets the same marker.
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Interactive runtime: whether an element mounting right now replaces prerendered markup. True
    /// only during the runtime's first render after a prerender that persisted the marker - the
    /// render that replaces the prerendered markup. Elements mounting after it are new content.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Restores a bool, which System.Text.Json deserializes without reflection over user types.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Restores a bool, which System.Text.Json deserializes without runtime code generation.")]
    internal bool IsHydrating
    {
        get
        {
            if (_hydrating is null)
            {
                bool restored = false;
                try
                {
                    restored = _state is not null && _state.TryTakeFromJson<bool>(StateKey, out var marker) && marker;
                }
                catch (InvalidOperationException)
                {
                    // Persisted state is no longer available to take from.
                }
                _hydrating = restored;
                if (restored) _ = EndHydrationAfterCurrentRenderAsync();
            }
            return _hydrating.Value;
        }
    }

    /// <summary>Closes the hydration window: everything mounting from now on is new content.</summary>
    internal void EndHydration() => _hydrating = false;

    // The first render replaces the whole prerendered page synchronously, so the window can close as
    // soon as control returns to the dispatcher. Elements whose parents await data mount in a later
    // render, as new content - the same flicker any prerendered Blazor page has unless that data is
    // persisted too.
    private async Task EndHydrationAfterCurrentRenderAsync()
    {
        await Task.Yield();
        EndHydration();
    }

    /// <summary>
    /// How long, in ms, the prerendered page's CSS animations have been running, or <c>null</c> when
    /// that can't be known synchronously (it can on WebAssembly). Read once: every element replacing
    /// prerendered markup does so in the same render.
    /// </summary>
    internal double? ElapsedMs
    {
        get
        {
            if (!_elapsedRead)
            {
                _elapsedRead = true;
                try { _elapsedMs = _interop.GetPrerenderedAnimationElapsedMs(); }
                catch { _elapsedMs = null; }
            }
            return _elapsedMs;
        }
    }

    public void Dispose() => _subscription.Dispose();
}
