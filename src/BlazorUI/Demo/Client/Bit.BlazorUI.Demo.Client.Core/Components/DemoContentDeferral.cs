namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>
/// Whether a component demo page may hold back the parts of itself the reader has not reached yet -
/// an example's live preview, the API tables - without having to measure anything first, and mount
/// them as they approach the viewport (see <c>observeVisibility</c> in Scripts/app.ts) or during the
/// browser's idle time (<c>requestIdleWork</c>, and DemoPage.QueueBackfill).
/// <para>
/// It is off for the first page the app paints and on for every page after it. Every page after the
/// first is built from nothing on the client, at the top of the document, with no prerendered copy to
/// contradict - which is exactly the navigation that freezes on a phone, and exactly where mounting
/// only what is in reach is free of consequences.
/// </para>
/// <para>
/// The first page is the prerendered one, and there the same trick has one condition: its HTML is
/// already in the document - it is what the crawlers and the social scrapers read, and what the
/// browser has restored a scroll position or an "#example12" fragment against - so a block may only
/// empty out if what replaces it stands exactly as tall as what the server sent. That is a
/// measurement, not a flag, so it is not this type's business: the page and the example take it
/// themselves, off their own prerendered markup, in the last moment before the client's first render
/// batch replaces it (see <c>TryGetElementHeight</c>). Prerendering itself and the MCP rendering are
/// never deferred at all - both owe their reader the whole page in one pass.
/// </para>
/// <para>
/// Registered scoped rather than held in a static, which is the difference between "this reader has
/// already been shown a page" and "this process has". Under Blazor Server a static would be shared
/// by every circuit, so the second visitor to the site would get a first page with its content held
/// back - deferred against a prerender they did see and a first paint they are still waiting for.
/// </para>
/// </summary>
public class DemoContentDeferral
{
    /// <summary>
    /// False until the first component demo page has finished rendering interactively, true from then
    /// until the next full reload.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Called by <see cref="DemoPage"/> once it has rendered on the client, which is the moment the
    /// prerendered page stops being the page on screen.
    /// </summary>
    public void Enable() => IsEnabled = true;
}
