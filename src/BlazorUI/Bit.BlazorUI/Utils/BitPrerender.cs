using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI;

/// <summary>
/// Whether a component renders what a visitor has already been shown by a prerender. A prerender (and a static SSR
/// page) is told apart by its JS runtime, which cannot be called yet; the interactive render that replaces it cannot
/// tell on its own, so the prerender has the page carry that over in its <see cref="PersistentComponentState"/>. A
/// component that hands something over from the browser at its first render (an image the browser was already
/// painting) does so only then, and a page that was never prerendered costs it nothing.
/// </summary>
internal static class BitPrerender
{
    private const string StateKey = "BitPrerendered";

    // Keyed on the state rather than held statically, so nothing outlives the request or the circuit it belongs to.
    // Components initialize and the persisting callbacks run on the renderer's dispatcher, so an entry is never
    // touched concurrently.
    private static readonly ConditionalWeakTable<PersistentComponentState, PageState> Pages = new();

    /// <summary>
    /// Whether the component rendering now is prerendered, or replaces what a prerender rendered. Once a page has
    /// been carried over from a prerender this keeps answering yes for the circuit (or the WebAssembly app) it
    /// started, since the persisted entry can be taken only once and a page reached later through an enhanced
    /// navigation is prerendered again - what it costs a page that was not is one look at the browser per component.
    /// </summary>
    internal static bool IsHandOver(IServiceProvider services, IJSRuntime js)
    {
        if (js.IsRuntimeInvalid())
        {
            CarryOver(services);

            return true;
        }

        if (services.GetService(typeof(PersistentComponentState)) is not PersistentComponentState state) return false;

        var page = Pages.GetValue(state, static _ => new PageState());

        if (page.Prerendered is false && TakePersisted(state))
        {
            page.Prerendered = true;
        }

        return page.Prerendered;
    }

    /// <summary>
    /// The first prerendered component of a state registers the one callback that persists the flag, with an explicit
    /// render mode: the framework infers the mode of a callback registered without one from the component it belongs
    /// to, and this callback belongs to the page, not to a component - it would throw. Auto is accepted by the server
    /// store and the WebAssembly store alike, so the entry reaches whichever runtime renders the page next.
    /// </summary>
    private static void CarryOver(IServiceProvider services)
    {
        if (services.GetService(typeof(PersistentComponentState)) is not PersistentComponentState state) return;

        var page = Pages.GetValue(state, static _ => new PageState());

        if (page.Subscribed) return;

        page.Subscribed = true;

        // The subscription lives as long as the state does - the request's - so it is never disposed on its own.
        _ = state.RegisterOnPersisting(() =>
        {
            Persist(state);

            return Task.CompletedTask;
        }, RenderMode.InteractiveAuto);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The persisted value is a bool.")]
    private static void Persist(PersistentComponentState state) => state.PersistAsJson(StateKey, true);

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The persisted value is a bool.")]
    private static bool TakePersisted(PersistentComponentState state) => state.TryTakeFromJson<bool>(StateKey, out var value) && value;

    private sealed class PageState
    {
        public bool Subscribed { get; set; }

        public bool Prerendered { get; set; }
    }
}
