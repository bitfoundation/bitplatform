using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Bit.Brouter;

/// <summary>How <see cref="BrouterLink"/> compares its <see cref="BrouterLink.Href"/> to the current URL.</summary>
public enum BrouterLinkMatch
{
    /// <summary>Match when the current path starts with the link's href (default).</summary>
    Prefix = 0,

    /// <summary>Match only when the current path equals the link's href exactly.</summary>
    All = 1
}

/// <summary>
/// An anchor element that automatically toggles an <c>active</c> class and <c>aria-current="page"</c>
/// when its <see cref="Href"/> matches the current URL. Equivalent to React Router's <c>NavLink</c>
/// and Vue Router's <c>router-link</c>.
/// </summary>
public sealed class BrouterLink : ComponentBase, IAsyncDisposable
{
    [Inject] private IBrouter Brouter { get; set; } = default!;
    [Inject] private IOptions<BrouterOptions> OptionsAccessor { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // Cache the resolved options to keep per-render reads cheap. IOptions<T>.Value is a
    // light call, but UpdateActiveState runs on every render of every link.
    private BrouterOptions Options => OptionsAccessor.Value;

    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// The destination URL or path. A route-relative path (<c>./x</c>, <c>../x</c>) is resolved
    /// against the current location using segment math (from <c>/users/42</c>, <c>"./edit"</c>
    /// points at <c>/users/42/edit</c> and <c>"../7"</c> at <c>/users/7</c>); the anchor's
    /// rendered <c>href</c> is the resolved absolute path and is re-resolved after every
    /// navigation. Bare paths without a leading <c>.</c> keep their base-relative meaning.
    /// </summary>
    [Parameter, EditorRequired] public string Href { get; set; } = "/";

    /// <summary>Inner content of the link.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Class always applied to the anchor.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Class applied in addition to <see cref="Class"/> when the link matches the current URL.</summary>
    [Parameter] public string ActiveClass { get; set; } = "active";

    /// <summary>How href is compared to the current URL.</summary>
    [Parameter] public BrouterLinkMatch Match { get; set; } = BrouterLinkMatch.Prefix;

    /// <summary>
    /// If true, navigation replaces the current history entry instead of pushing a new one.
    /// Modified clicks (Ctrl/Cmd+click, Shift+click, etc.) and non-primary clicks fall through
    /// to the browser's default behavior (e.g., "open in new tab"); only unmodified left-clicks
    /// are intercepted to perform the replace navigation.
    /// </summary>
    [Parameter] public bool Replace { get; set; }

    /// <summary>
    /// When (if ever) this link preloads its destination's loader data into the router's cache so
    /// the actual navigation finds warm data: on interaction <see cref="BrouterLinkPreload.Intent"/>
    /// (hover/touch/focus, debounced by <see cref="BrouterOptions.PreloadDelay"/>), when scrolled
    /// <see cref="BrouterLinkPreload.Viewport"/> into view, or at <see cref="BrouterLinkPreload.Render"/>
    /// time. Null (the default) falls back to <see cref="BrouterOptions.DefaultLinkPreload"/>.
    /// Preloads run loaders only - no guards, no rendering; see
    /// <see cref="BrouterNavigationContext.IsPreload"/>.
    /// </summary>
    [Parameter] public BrouterLinkPreload? Preload { get; set; }

    /// <summary>
    /// Optional application state to attach to the destination's history entry when this link is
    /// clicked (see <see cref="IBrouter.Navigate"/>). Read it back on
    /// <see cref="BrouterLocation.HistoryState"/> after the navigation - including when the user
    /// later returns to the entry via Back/Forward. Setting this makes the link intercept
    /// unmodified left-clicks the same way <see cref="Replace"/> does (an href-driven navigation
    /// cannot carry history state); modified clicks keep their native browser behavior, in which
    /// case the state is not attached (a new tab is a fresh history stack anyway).
    /// </summary>
    [Parameter] public string? HistoryState { get; set; }


    private bool _isActive;
    // Href with any route-relative prefix ("./", "../") resolved against the current location.
    // Equals Href verbatim for absolute/base-relative hrefs. This is what gets rendered into
    // the anchor, navigated to on click, and matched for the active state, so all three always
    // agree. Recomputed in UpdateActiveState; re-rendered when navigation changes it.
    private string _resolvedHref = "/";
    private ElementReference _anchor;
    // Fallback module import owned by THIS link, used only when Brouter isn't the shipped
    // BrouterService (a custom IBrouter implementation). The normal path shares the scope's
    // single module via BrouterService.GetModuleAsync, so a page full of Replace links costs
    // one interop import instead of one per link. Only this fallback is disposed here; the
    // shared module belongs to the service.
    private IJSObjectReference? _module;
    private IJSObjectReference? _handle;
    private bool _interceptWired;

    // Replace and HistoryState both require Brouter (not href-driven NavigationInterception) to
    // perform the navigation, so both use the same conditional-preventDefault click interception.
    private bool NeedsClickInterception => Replace || HistoryState is not null;

    private BrouterLinkPreload EffectivePreload => Preload ?? Options.DefaultLinkPreload;

    // Intent/Viewport need DOM listeners; Render fires straight from OnAfterRenderAsync.
    private bool NeedsPreloadWiring =>
        EffectivePreload is BrouterLinkPreload.Intent or BrouterLinkPreload.Viewport;

    // JS handle for the preload trigger wiring (separate from the click-interception handle) and
    // the .NET reference its callbacks target.
    private IJSObjectReference? _preloadHandle;
    private DotNetObjectReference<BrouterLink>? _selfRef;
    private bool _preloadWired;
    private bool _renderPreloadFired;

    // UpdateActiveState memoisation. UpdateActiveState runs on every render of every link
    // (OnParametersSet) and on every successful navigation (OnNavigated). For pages with many
    // nav links this multiplies. Cache the inputs the last computation observed; on a re-call
    // with the same inputs we just keep _isActive untouched and return.
    private string? _cachedCurrent;
    private string? _cachedHref;
    private BrouterLinkMatch? _cachedMatch;
    private string? _cachedTarget; // normalised form of _cachedHref

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Brouter.OnNavigated += OnNavigated;
        // Note: no UpdateActiveState() here. OnParametersSet runs immediately after
        // OnInitialized for every parameter pass and unconditionally calls UpdateActiveState,
        // so doing it twice on the very first pass is wasted work.
    }

    protected override void OnParametersSet()
    {
        UpdateActiveState();
        base.OnParametersSet();
    }

    private ValueTask OnNavigated(BrouterNavigationContext ctx)
    {
        var wasActive = _isActive;
        var wasHref = _resolvedHref;
        UpdateActiveState();
        // Return the InvokeAsync task wrapped as a ValueTask so any exception thrown by the
        // re-render flows up the OnNavigated invocation chain instead of becoming an unobserved
        // task. ValueTask.CompletedTask is correct when nothing changed. A changed resolved
        // href (a route-relative Href pointing somewhere new after this navigation) needs a
        // re-render even when the active flag didn't move, so the DOM href stays current.
        return _isActive == wasActive && string.Equals(_resolvedHref, wasHref, StringComparison.Ordinal)
            ? ValueTask.CompletedTask
            : new ValueTask(InvokeAsync(StateHasChanged));
    }

    private void UpdateActiveState()
    {
        var current = Brouter.Location.Path;

        // Fast path: same current path, same Href, same Match -> result hasn't changed.
        // String reference equality on Brouter.Location.Path is reasonable because
        // BrouterLocation is rebuilt only on navigation (via ComputeLocation), and within a
        // single navigation every link reads the same instance. Fall through to the value
        // equality comparison for the rare case where two distinct strings happen to be equal.
        if (_cachedHref is not null
            && ReferenceEquals(_cachedHref, Href)
            && _cachedMatch == Match
            && (ReferenceEquals(_cachedCurrent, current)
                || string.Equals(_cachedCurrent, current, StringComparison.Ordinal)))
        {
            return;
        }

        // Recompute. A route-relative Href depends on the current path, so resolve it first;
        // for absolute/base-relative hrefs this is Href verbatim.
        var resolvedHref = BrouterRelativeUrl.ResolveIfRelative(current, Href);

        // Target only needs renormalising when the resolved href actually changed (value
        // comparison, not reference: the resolution may rebuild an equal string).
        string target;
        if (_cachedTarget is not null && string.Equals(_resolvedHref, resolvedHref, StringComparison.Ordinal))
        {
            target = _cachedTarget;
        }
        else
        {
            // Brouter.ComputeLocation() only strips a trailing slash from Path when
            // Options.IgnoreTrailingSlash is true, so we must mirror that here when normalising
            // the link's Href. Otherwise BrouterLinkMatch.All would never match a current path
            // that legitimately ends in '/' under Options.IgnoreTrailingSlash == false.
            target = NormalisePath(resolvedHref, stripTrailingSlash: Options.IgnoreTrailingSlash);
            _cachedTarget = target;
        }
        _resolvedHref = resolvedHref;
        var comparison = Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        _isActive = Match switch
        {
            BrouterLinkMatch.All => string.Equals(current, target, comparison),
            // The root "/" prefix-matches every path (everything starts with '/'), so a "home"
            // link would light up on every page - the classic NavLink footgun. Match the root
            // exactly even under Prefix, mirroring React Router's NavLink (a link to "/" is only
            // active at the root). Note both the old `target == "/"` clause AND the `target[^1] ==
            // '/'` clause below would otherwise force the root to always match.
            _ when target == "/" => string.Equals(current, target, comparison),
            // Prefix match: when target retains a trailing '/' (Options.IgnoreTrailingSlash == false
            // and the link href ended with '/'), the slash itself enforces the segment boundary,
            // so the explicit boundary check on current[target.Length] is unnecessary in that case.
            _ => current.StartsWith(target, comparison) &&
                 (current.Length == target.Length || target[^1] == '/' ||
                  current[target.Length] == '/' || current[target.Length] == '?' || current[target.Length] == '#')
        };

        _cachedCurrent = current;
        _cachedHref = Href;
        _cachedMatch = Match;
    }

    private static string NormalisePath(string href, bool stripTrailingSlash)
    {
        string path;
        if (Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath;
        }
        else
        {
            path = href;
            var hashIdx = path.IndexOf('#');
            if (hashIdx >= 0) path = path[..hashIdx];
            var qIdx = path.IndexOf('?');
            if (qIdx >= 0) path = path[..qIdx];
        }
        if (stripTrailingSlash && path.Length > 1 && path.EndsWith('/')) path = path[..^1];
        if (path.Length == 0 || path[0] != '/') path = "/" + path;
        return path;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Merge caller-provided "class" from AdditionalAttributes with our Class/ActiveClass so
        // splatted classes don't get clobbered by the explicit class attribute we add below.
        string? extraClass = null;
        if (AdditionalAttributes is not null &&
            AdditionalAttributes.TryGetValue("class", out var raw) &&
            raw is string s && string.IsNullOrWhiteSpace(s) is false)
        {
            extraClass = s;
        }

        var ownClass = string.IsNullOrEmpty(Class)
            ? (_isActive ? ActiveClass : null)
            : (_isActive ? $"{Class} {ActiveClass}".Trim() : Class);

        var combinedClass = (extraClass, ownClass) switch
        {
            (null, null) => null,
            (null, _) => ownClass,
            (_, null) => extraClass,
            _ => $"{extraClass} {ownClass}"
        };

        builder.OpenElement(0, "a");
        if (AdditionalAttributes is not null)
        {
            // Filter "class" out of the splatted attributes: we've already merged its value
            // into combinedClass above and emit a single explicit class attribute below.
            // Splatting the original dictionary as well would produce a redundant class frame
            // that relies on Blazor's last-write-wins diff rule to be neutralised.
            if (extraClass is null)
            {
                builder.AddMultipleAttributes(1, AdditionalAttributes);
            }
            else
            {
                builder.AddMultipleAttributes(1, AdditionalAttributes.Where(kv => string.Equals(kv.Key, "class", StringComparison.OrdinalIgnoreCase) is false).Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value)));
            }
        }
        builder.AddAttribute(2, "href", _resolvedHref);
        if (combinedClass is not null) builder.AddAttribute(3, "class", combinedClass);
        if (_isActive) builder.AddAttribute(4, "aria-current", "page");

        // Without Replace/HistoryState we rely on Blazor's NavigationInterception (same as
        // Microsoft's NavLink) to drive navigation off the anchor's href.
        // For Replace/HistoryState we hook our own click handler in C# AND wire a JS capture-phase
        // listener (see OnAfterRenderAsync) that conditionally calls preventDefault only for
        // unmodified primary clicks. That way, modified clicks (Ctrl/Cmd+click, Shift+click)
        // keep their native "open in new tab" / "open in new window" behavior; only plain
        // left-clicks are intercepted to perform the replace navigation.
        // We deliberately do NOT set onclick:stopPropagation here. When the JS handler is
        // installed, Blazor's document-level NavigationInterception self-skips because the
        // capture-phase listener has already called preventDefault (it checks defaultPrevented),
        // so no double navigation occurs. When wiring isn't available (pre-render, circuit
        // disconnect, or interop failure), letting the click bubble means NavigationInterception
        // can still pick it up and perform an SPA push navigation as a graceful fallback,
        // instead of falling all the way through to a full page load.
        if (NeedsClickInterception)
        {
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
        }
        if (NeedsClickInterception || NeedsPreloadWiring)
        {
            builder.AddElementReferenceCapture(6, capturedRef => _anchor = capturedRef);
        }

        builder.AddContent(7, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Note: no ConfigureAwait(false) below. In a Blazor component lifecycle method we want
        // to stay on the renderer's SynchronizationContext so subsequent JS interop calls and
        // any state changes/StateHasChanged remain marshaled correctly (especially on Blazor
        // Server, where leaving the renderer context can break interop/state updates).
        if (NeedsClickInterception && _interceptWired is false)
        {
            try
            {
                var module = await GetModuleAsync();
                _handle = await module.InvokeAsync<IJSObjectReference>(
                    "wireConditionalPreventDefault", _anchor);
                _interceptWired = true;
            }
            catch (JSDisconnectedException) { /* Circuit disconnected; nothing to wire. */ }
            catch (JSException) { /* JS interop failure; falls back to default link behavior. */ }
            // InvalidOperationException also covers ObjectDisposedException, thrown when the
            // shared module is disposed during scope teardown while a link is still wiring.
            catch (InvalidOperationException) { /* JS interop unavailable during pre-render. */ }
            catch (TaskCanceledException) { /* Component disposed mid-call. */ }
        }
        else if (NeedsClickInterception is false && _interceptWired)
        {
            // Replace/HistoryState switched off after wiring; tear the JS handler down.
            await DisposeJsHandleAsync();
            _interceptWired = false;
        }

        // Preload wiring, independent of the click-interception handle above.
        switch (EffectivePreload)
        {
            case BrouterLinkPreload.Render when _renderPreloadFired is false:
                _renderPreloadFired = true;
                // Fire-and-forget: preloading is speculative and must never block rendering.
                _ = Brouter.PreloadAsync(_resolvedHref).AsTask();
                break;

            case BrouterLinkPreload.Intent or BrouterLinkPreload.Viewport when _preloadWired is false:
                try
                {
                    var module = await GetModuleAsync();
                    _selfRef ??= DotNetObjectReference.Create(this);
                    _preloadHandle = await module.InvokeAsync<IJSObjectReference>(
                        "wirePreload", _anchor,
                        EffectivePreload == BrouterLinkPreload.Intent ? "intent" : "viewport",
                        Options.PreloadDelay.TotalMilliseconds, _selfRef);
                    _preloadWired = true;
                }
                catch (JSDisconnectedException) { /* circuit disconnected; nothing to wire */ }
                catch (JSException) { /* JS interop failure; preloading degrades to nothing */ }
                catch (InvalidOperationException) { /* interop unavailable during pre-render */ }
                catch (TaskCanceledException) { /* component disposed mid-call */ }
                break;
        }
    }

    /// <summary>JS-invoked when the wired preload trigger (intent/viewport) fires.</summary>
    [JSInvokable]
    public Task OnPreloadTriggered() => Brouter.PreloadAsync(_resolvedHref).AsTask();

    private ValueTask<IJSObjectReference> GetModuleAsync()
    {
        // Prefer the scope-shared module owned by BrouterService: every Replace link on the
        // page then reuses one import instead of paying an interop round-trip each. The
        // per-link import only remains for custom IBrouter implementations, where no shared
        // module exists.
        if (Brouter is BrouterService service) return service.GetModuleAsync();

        return ImportOwnModuleAsync();

        async ValueTask<IJSObjectReference> ImportOwnModuleAsync() =>
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Bit.Brouter/bit-brouter.js");
    }

    private void OnClick(MouseEventArgs e)
    {
        // Mirrors the JS-side filter so the C# logic agrees with what the browser is doing:
        // for modified or non-primary clicks the JS listener doesn't preventDefault, the
        // browser opens the link natively, and we should not also push a replace navigation.
        if (e.Button != 0 || e.CtrlKey || e.ShiftKey || e.AltKey || e.MetaKey) return;

        // Only issue our own navigation when the JS preventDefault handler is installed.
        // Otherwise Blazor's NavigationInterception will pick the click up as a regular push
        // navigation (we no longer stopPropagation, so the document-level interceptor still
        // sees the event), and adding our own NavigateTo here would result in double-navigation
        // (two LocationChanged events / two ProcessNavigationAsync passes for one click).
        // Degrading to a plain, state-less push when wiring failed is the safer fallback than
        // racing with the built-in interceptor or forcing a full page load.
        if (_interceptWired is false) return;

        // Navigate to the resolved href (identical to Href for non-relative links) so the
        // click goes exactly where the rendered anchor points.
        Brouter.Navigate(_resolvedHref, replace: Replace, historyState: HistoryState);
    }

    private async ValueTask DisposeJsHandleAsync()
    {
        if (_handle is not null)
        {
            try { await _handle.InvokeVoidAsync("dispose"); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }

            try { await _handle.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }

            _handle = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Brouter.OnNavigated -= OnNavigated;

        await DisposeJsHandleAsync();

        if (_preloadHandle is not null)
        {
            try { await _preloadHandle.InvokeVoidAsync("dispose"); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }

            try { await _preloadHandle.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }

            _preloadHandle = null;
        }
        _selfRef?.Dispose();
        _selfRef = null;

        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
            _module = null;
        }
    }
}
