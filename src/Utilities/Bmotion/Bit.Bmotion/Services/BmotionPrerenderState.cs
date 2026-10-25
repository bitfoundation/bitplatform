using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion;

/// <summary>
/// Tells the interactive runtime which elements the prerendering pass rendered at rest - one per DI
/// scope.
/// <para>
/// Blazor doesn't hydrate prerendered markup: the interactive renderer throws it away and renders
/// every component again. An element prerendered at rest (<see cref="BmPrerenderMode.Final"/> /
/// <see cref="BmPrerenderMode.Animate"/>) would therefore jump back to <c>Initial</c> and play its
/// enter animation a second time. To prevent that, each such element persists an entry through
/// <see cref="PersistentComponentState"/>, and an element mounting in the runtime's first render
/// claims one and renders straight into its resting state.
/// </para>
/// <para>
/// Entries are keyed by what the element looks like at rest, not by where it sits in the tree: the
/// runtime may render a little differently from the prerendering pass, and a runtime only sees the
/// entries of its own interactive islands. An element nothing was prerendered for - content that
/// only exists once interactive - finds no entry and enters normally; elements that rest alike are
/// interchangeable, as far as the page shows.
/// </para>
/// </summary>
internal sealed class BmotionPrerenderState : IDisposable
{
    private const string KeyPrefix = "Bit.Bmotion.Prerendered:";

    private readonly PersistentComponentState? _state;
    private readonly List<PersistingComponentStateSubscription> _persisting = [];
#if NET10_0_OR_GREATER
    private readonly RestoringComponentStateSubscription _restoring;
#endif
    // Prerendering pass: how many elements have rested alike so far, per resting signature.
    private readonly Dictionary<string, int> _prerendered = new(StringComparer.Ordinal);
    // Interactive runtime: per resting signature, how many entries the prerendering pass persisted
    // and the next one to try.
    private readonly Dictionary<string, (int Count, int Next)> _claims = new(StringComparer.Ordinal);
    private HydrationWindow _window;

    private enum HydrationWindow { NotStarted, Open, Closed }

    public BmotionPrerenderState(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        // Optional: hosts that never prerender (e.g. a BlazorWebView) may not register it at all.
        _state = services.GetService<PersistentComponentState>();
#if NET10_0_OR_GREATER
        // Enhanced navigation can bring new prerendered islands - and their entries - to a runtime
        // that is already running: their first render is a hydration window of its own.
        try
        {
            _restoring = _state?.RegisterOnRestoring(Reopen, new RestoreOptions { AllowUpdates = true }) ?? default;
        }
        catch (InvalidOperationException)
        {
            // A state that doesn't support restoration callbacks: only the first render hydrates.
        }
#endif
    }

    /// <summary>The signature elements resting alike share: what they render at rest, and how.</summary>
    internal static string Signature(BmPrerenderMode mode, string settledStyle)
        => $"{(int)mode}|{settledStyle}";

    // ── Prerendering pass ──────────────────────────────────────────────────────

    /// <summary>
    /// Records an element prerendered at rest and registers <paramref name="persist"/> to write its
    /// entry, which must be a method of the element itself: Blazor sends persisted state only to the
    /// interactive runtime of the component that registered the callback, so each element's entry
    /// reaches the runtime that will render it again. Returns the element's ordinal among those
    /// resting alike, to hand to <see cref="PersistEntry"/>; <c>null</c> when nothing can be persisted.
    /// </summary>
    internal int? MarkPrerendered(string signature, Func<Task> persist)
    {
        if (_state is null) return null;
        try
        {
            _persisting.Add(_state.RegisterOnPersisting(persist));
        }
        catch (InvalidOperationException)
        {
            // The state has already been persisted (a component rendered after the response's
            // state was written); the runtime will animate the element as if nothing was prerendered.
            return null;
        }
        int ordinal = _prerendered.GetValueOrDefault(signature);
        _prerendered[signature] = ordinal + 1;
        return ordinal;
    }

    /// <summary>
    /// Writes an element's entry - the id its prerendered markup carries - plus how many elements
    /// rested alike. Invoked through the callback registered in <see cref="MarkPrerendered"/>, once
    /// per runtime the element is persisted for.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Persists a string and an int, which System.Text.Json serializes without reflection over user types.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Persists a string and an int, which System.Text.Json serializes without runtime code generation.")]
    internal Task PersistEntry(string signature, int ordinal, string elementId)
    {
        if (_state is null) return Task.CompletedTask;
        try
        {
            _state.PersistAsJson(EntryKey(signature, ordinal), elementId);
        }
        catch (ArgumentException)
        {
            // Already persisted for this runtime.
        }
        try
        {
            _state.PersistAsJson(CountKey(signature), _prerendered.GetValueOrDefault(signature));
        }
        catch (ArgumentException)
        {
            // Another element resting alike already wrote the count for this runtime.
        }
        return Task.CompletedTask;
    }

    // ── Interactive runtime ────────────────────────────────────────────────────

    /// <summary>
    /// Called by every element mounting in the interactive runtime. The first one opens the
    /// hydration window: the render it belongs to is the one replacing the prerendered markup.
    /// </summary>
    internal void NoteMount()
    {
        if (_window != HydrationWindow.NotStarted) return;
        _window = HydrationWindow.Open;
        _ = EndHydrationAfterCurrentRenderAsync();
    }

    /// <summary>
    /// Whether an element mounting right now may replace prerendered markup: true only during the
    /// render that replaces it. Elements mounting after it are new content.
    /// </summary>
    internal bool IsHydrating => _window == HydrationWindow.Open;

    /// <summary>Closes the hydration window: everything mounting from now on is new content.</summary>
    internal void EndHydration() => _window = HydrationWindow.Closed;

    /// <summary>
    /// Claims the entry of a prerendered element that rested like this one, returning the id its
    /// prerendered markup carries; <c>null</c> when none is left or the window has closed.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Restores a string and an int, which System.Text.Json deserializes without reflection over user types.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Restores a string and an int, which System.Text.Json deserializes without runtime code generation.")]
    internal string? Claim(string signature)
    {
        if (!IsHydrating || _state is null) return null;
        try
        {
            if (!_claims.TryGetValue(signature, out var claim))
                claim = (_state.TryTakeFromJson<int>(CountKey(signature), out var count) ? count : 0, 0);

            string? elementId = null;
            // Entries persisted for another runtime (or skipped) leave gaps: try each in turn.
            while (elementId is null && claim.Next < claim.Count)
            {
                if (!_state.TryTakeFromJson(EntryKey(signature, claim.Next++), out elementId)) elementId = null;
            }
            _claims[signature] = claim;
            return elementId;
        }
        catch (InvalidOperationException)
        {
            // Persisted state is no longer available to take from.
            return null;
        }
    }

    // The first render replaces the whole prerendered page synchronously, so the window can close as
    // soon as control returns to the dispatcher. Elements whose parents await data mount in a later
    // render, as new content - the same flicker any prerendered Blazor page has unless that data is
    // persisted too.
    private async Task EndHydrationAfterCurrentRenderAsync()
    {
        await Task.Yield();
        EndHydration();
    }

    private void Reopen()
    {
        _window = HydrationWindow.NotStarted;
        _claims.Clear();
    }

    internal static string EntryKey(string signature, int ordinal) => $"{KeyPrefix}{Hash(signature)}:{ordinal}";

    internal static string CountKey(string signature) => $"{KeyPrefix}{Hash(signature)}";

    // Stable across processes (the prerendering server and a WebAssembly runtime must agree), unlike
    // string.GetHashCode, and short enough to keep the persisted state small.
    private static string Hash(string signature)
    {
        ulong hash = 14695981039346656037;
        foreach (char c in signature)
        {
            hash ^= c;
            hash *= 1099511628211;
        }
        return hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
    }

    public void Dispose()
    {
        foreach (var subscription in _persisting) subscription.Dispose();
        _persisting.Clear();
#if NET10_0_OR_GREATER
        _restoring.Dispose();
#endif
    }
}
