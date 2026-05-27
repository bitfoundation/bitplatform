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

    /// <summary>The destination URL or path.</summary>
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


    private bool _isActive;
    private ElementReference _anchor;
    private IJSObjectReference? _module;
    private IJSObjectReference? _handle;
    private bool _replaceWired;

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
        var was = _isActive;
        UpdateActiveState();
        // Return the InvokeAsync task wrapped as a ValueTask so any exception thrown by the
        // re-render flows up the OnNavigated invocation chain instead of becoming an unobserved
        // task. ValueTask.CompletedTask is correct when nothing changed.
        return _isActive == was
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

        // Recompute. Target only needs renormalising when Href actually changed.
        string target;
        if (_cachedTarget is not null && ReferenceEquals(_cachedHref, Href))
        {
            target = _cachedTarget;
        }
        else
        {
            // Brouter.ComputeLocation() only strips a trailing slash from Path when
            // Options.IgnoreTrailingSlash is true, so we must mirror that here when normalising
            // the link's Href. Otherwise BrouterLinkMatch.All would never match a current path
            // that legitimately ends in '/' under Options.IgnoreTrailingSlash == false.
            target = NormalisePath(Href, stripTrailingSlash: Options.IgnoreTrailingSlash);
            _cachedTarget = target;
        }
        var comparison = Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        _isActive = Match switch
        {
            BrouterLinkMatch.All => string.Equals(current, target, comparison),
            // Prefix match: when target retains a trailing '/' (Options.IgnoreTrailingSlash == false
            // and the link href ended with '/'), the slash itself enforces the segment boundary,
            // so the explicit boundary check on current[target.Length] is unnecessary in that case.
            _ => current.StartsWith(target, comparison) &&
                 (current.Length == target.Length || target == "/" || target[^1] == '/' ||
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
        builder.AddAttribute(2, "href", Href);
        if (combinedClass is not null) builder.AddAttribute(3, "class", combinedClass);
        if (_isActive) builder.AddAttribute(4, "aria-current", "page");

        // For Replace=false we rely on Blazor's NavigationInterception (same as Microsoft's
        // NavLink) to drive navigation off the anchor's href.
        // For Replace=true we hook our own click handler in C# AND wire a JS capture-phase
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
        if (Replace)
        {
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
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
        if (Replace && _replaceWired is false)
        {
            try
            {
                _module ??= await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/Bit.Brouter/BitBrouter.js");
                _handle = await _module.InvokeAsync<IJSObjectReference>(
                    "wireConditionalPreventDefault", _anchor);
                _replaceWired = true;
            }
            catch (JSDisconnectedException) { /* Circuit disconnected; nothing to wire. */ }
            catch (JSException) { /* JS interop failure; falls back to default link behavior. */ }
            catch (InvalidOperationException) { /* JS interop unavailable during pre-render. */ }
            catch (TaskCanceledException) { /* Component disposed mid-call. */ }
        }
        else if (Replace is false && _replaceWired)
        {
            // Replace switched off after wiring; tear the JS handler down.
            await DisposeJsHandleAsync();
            _replaceWired = false;
        }
    }

    private void OnClick(MouseEventArgs e)
    {
        // Mirrors the JS-side filter so the C# logic agrees with what the browser is doing:
        // for modified or non-primary clicks the JS listener doesn't preventDefault, the
        // browser opens the link natively, and we should not also push a replace navigation.
        if (e.Button != 0 || e.CtrlKey || e.ShiftKey || e.AltKey || e.MetaKey) return;

        // Only issue the replace navigation when our JS preventDefault handler is installed.
        // Otherwise Blazor's NavigationInterception will pick the click up as a regular push
        // navigation (we no longer stopPropagation, so the document-level interceptor still
        // sees the event), and adding our own NavigateTo here would result in double-navigation
        // (two LocationChanged events / two ProcessNavigationAsync passes for one click).
        // Degrading to a push when wiring failed is the safer fallback than racing with the
        // built-in interceptor or forcing a full page load.
        if (_replaceWired is false) return;

        Brouter.Navigate(Href, replace: true);
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
