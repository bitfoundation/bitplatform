using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Speculation_Rules_API">Speculation Rules API</see>
/// and the prerendering state that comes with it: <c>document.prerendering</c>, the
/// <c>prerenderingchange</c> event, and the activation timing.
/// </summary>
/// <remarks>
/// Two halves, and the second is the one every app needs.
/// <br/>
/// Adding rules asks the browser to prefetch or prerender URLs before the user clicks them, which is
/// how a navigation becomes instant. Prerendering runs the whole page - scripts, requests, timers -
/// in a hidden tab that may never be shown.
/// <br/>
/// Which is why the reading half matters even for an app that adds no rules of its own: the browser,
/// or the site that linked to you, may prerender your page anyway. While
/// <see cref="IsPrerendering"/> is true, nothing the page does is visible to anyone: analytics
/// events, a <c>play()</c>, a POST, anything that counts a visit is happening in a tab the user has
/// not opened. Hold those back and run them from <see cref="OnActivated"/> instead - which fires the
/// moment the prerender becomes the real page, or never, if it doesn't.
/// </remarks>
[ButilService(typeof(Speculation))]
public class Speculation(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokePrerenderingChange);

    private readonly ConcurrentDictionary<Guid, Action<double>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Speculation>? _dotNetRef;
    private DotNetObjectReference<Speculation> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime supports speculation rules.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.speculation.isSupported");

    /// <summary>
    /// True while this document is being prerendered in the background - it is running, but nobody is
    /// looking at it yet.
    /// </summary>
    /// <remarks>
    /// Note this is the browser's prerender, not Blazor's prerendering render mode: it means "a hidden
    /// tab", not "on the server". The two are unrelated and can both be happening.
    /// </remarks>
    public ValueTask<bool> IsPrerendering() => js.Invoke<bool>("BitButil.speculation.isPrerendering");

    /// <summary>
    /// Whether this page was prerendered before the user arrived at it.
    /// </summary>
    /// <remarks>Worth logging: it is the difference between a page load the user waited for and one they didn't.</remarks>
    public ValueTask<bool> WasPrerendered() => js.Invoke<bool>("BitButil.speculation.wasPrerendered");

    /// <summary>
    /// When the prerender began, relative to the moment the user activated the page - in
    /// milliseconds, and 0 for a page that was never prerendered.
    /// </summary>
    /// <remarks>
    /// Every other timestamp on the page is measured from the prerender's start, so a load time
    /// computed without subtracting this counts time the user never spent waiting.
    /// </remarks>
    public ValueTask<double> GetActivationStart() => js.Invoke<double>("BitButil.speculation.activationStart");

    /// <summary>
    /// Asks the browser to prerender these URLs: it loads and runs each page in a hidden tab, so a
    /// later navigation to one is instant.
    /// </summary>
    /// <param name="urls">Same-origin URLs. A handful you are confident about - each one is a whole page load the user may never use.</param>
    /// <param name="eagerness">How keen to be. See <see cref="SpeculationEagerness"/>.</param>
    /// <returns>A subscription - dispose it to remove the rules, cancelling any speculation they started that hasn't been used.</returns>
    /// <remarks>
    /// A prerendered page runs its scripts, so the pages you name should be ones that behave under
    /// <see cref="IsPrerendering"/> - which is the same discipline this class's own remarks describe.
    /// </remarks>
    public ValueTask<ButilSubscription?> Prerender(string[] urls, SpeculationEagerness eagerness = SpeculationEagerness.Moderate)
        => AddRules(BuildRules("prerender", urls, eagerness));

    /// <summary>
    /// Asks the browser to prefetch these URLs: it fetches the response and stops there, without
    /// running anything.
    /// </summary>
    /// <param name="urls">Same-origin URLs.</param>
    /// <param name="eagerness">How keen to be. See <see cref="SpeculationEagerness"/>.</param>
    /// <returns>A subscription - dispose it to remove the rules.</returns>
    /// <remarks>
    /// Much cheaper than <see cref="Prerender"/> and with none of its side effects, because the page
    /// is never executed - the safe one to be generous with.
    /// </remarks>
    public ValueTask<ButilSubscription?> Prefetch(string[] urls, SpeculationEagerness eagerness = SpeculationEagerness.Moderate)
        => AddRules(BuildRules("prefetch", urls, eagerness));

    /// <summary>
    /// Adds a speculation-rules document verbatim, for the shapes the two convenience methods don't
    /// cover - document rules that match links by selector, cross-site prefetches, per-rule
    /// referrer policies.
    /// </summary>
    /// <param name="rulesJson">
    /// The rule set, e.g.
    /// <c>{"prerender":[{"where":{"selector_matches":".product-link"},"eagerness":"moderate"}]}</c>.
    /// </param>
    /// <returns>A subscription - dispose it to remove the rules - or null when the runtime doesn't support speculation rules.</returns>
    public async ValueTask<ButilSubscription?> AddRules(string rulesJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesJson);

        var id = Guid.NewGuid();
        var added = await js.Invoke<bool>("BitButil.speculation.addRules", id, rulesJson);
        if (added is false) return null;

        return new ButilSubscription(id, async () => await js.InvokeVoid("BitButil.speculation.removeRules", id));
    }

    /// <summary>
    /// Invoked from JS when a prerendered document is activated. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokePrerenderingChange(Guid id, double activationStart)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(activationStart);
    }

    /// <summary>
    /// Calls <paramref name="handler"/> when a prerendered page is activated - the moment it stops
    /// being a hidden tab and becomes the page the user is looking at.
    /// </summary>
    /// <param name="handler">
    /// Called once, with the activation start in milliseconds. It never fires on a page that was not
    /// prerendered, so it is a place to put work, not the only place: pair it with
    /// <see cref="IsPrerendering"/> being false.
    /// </param>
    /// <returns>A subscription - dispose it to stop listening.</returns>
    /// <remarks>
    /// Where the work held back during prerendering belongs: the analytics page-view, the "mark as
    /// read" call, the autoplay. Doing it here is what makes the count match what the user actually saw.
    /// </remarks>
    [DynamicDependency(nameof(InvokePrerenderingChange), typeof(Speculation))]
    public async ValueTask<ButilSubscription> OnActivated(Action<double> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.speculation.onPrerenderingChange", DotNetRef, InvokeMethodName, id);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.speculation.offPrerenderingChange", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, removes any rule sets and activation listeners whose
    /// <see cref="ButilSubscription"/> was never disposed - a rule set left behind would keep asking
    /// the browser to load pages for a component that is gone.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.speculation.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The rule document for a list of URLs. Built by hand rather than serialized: the payload is
    /// three fixed keys and a list of strings, and going through <see cref="JsonSerializer"/> for that
    /// would mean either a DTO the trimmer has to be told about or an annotated generic call.
    /// <see cref="JsonEncodedText"/> does the escaping, which is the only part worth not writing.
    /// </summary>
    private static string BuildRules(string action, string[] urls, SpeculationEagerness eagerness)
    {
        ArgumentNullException.ThrowIfNull(urls);

        var builder = new StringBuilder();
        builder.Append("{\"").Append(action).Append("\":[{\"urls\":[");

        for (var i = 0; i < urls.Length; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append('"').Append(JsonEncodedText.Encode(urls[i] ?? string.Empty)).Append('"');
        }

        builder.Append("],\"eagerness\":\"").Append(ToName(eagerness)).Append("\"}]}");
        return builder.ToString();
    }

    private static string ToName(SpeculationEagerness eagerness) => eagerness switch
    {
        SpeculationEagerness.Immediate => "immediate",
        SpeculationEagerness.Eager => "eager",
        SpeculationEagerness.Conservative => "conservative",
        _ => "moderate",
    };
}
