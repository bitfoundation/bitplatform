using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
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
    [Inject] private BrouterOptions Options { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object>? AdditionalAttributes { get; set; }

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

    protected override void OnInitialized()
    {
        Brouter.OnNavigated += OnNavigated;
        UpdateActiveState();
    }

    protected override void OnParametersSet()
    {
        UpdateActiveState();
        base.OnParametersSet();
    }

    private ValueTask OnNavigated(NavigationContext ctx)
    {
        var was = _isActive;
        UpdateActiveState();
        if (was != _isActive) InvokeAsync(StateHasChanged);
        return ValueTask.CompletedTask;
    }

    private void UpdateActiveState()
    {
        var current = Brouter.Location.Path;
        // Brouter.UpdateLocation() only strips a trailing slash from Path when
        // Options.IgnoreTrailingSlash is true, so we must mirror that here when normalising
        // the link's Href. Otherwise BrouterLinkMatch.All would never match a current path
        // that legitimately ends in '/' under Options.IgnoreTrailingSlash == false.
        var target = NormalisePath(Href, stripTrailingSlash: Options.IgnoreTrailingSlash);
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
        if (AdditionalAttributes is not null) builder.AddMultipleAttributes(1, AdditionalAttributes);
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
        if (Replace)
        {
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
            builder.AddAttribute(6, "onclick:stopPropagation", true);
            builder.AddElementReferenceCapture(7, capturedRef => _anchor = capturedRef);
        }

        builder.AddContent(8, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Replace && _replaceWired is false)
        {
            try
            {
                _module ??= await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/Bit.Brouter/BitBrouter.js").ConfigureAwait(false);
                _handle = await _module.InvokeAsync<IJSObjectReference>(
                    "wireConditionalPreventDefault", _anchor).ConfigureAwait(false);
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
            await DisposeJsHandleAsync().ConfigureAwait(false);
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
        // navigation, and adding our own NavigateTo here would result in double-navigation
        // (two LocationChanged events / two ProcessNavigationAsync passes for one click).
        // Degrading to a push when wiring failed is the safer fallback than racing with the
        // built-in interceptor.
        if (_replaceWired is false) return;

        Brouter.Navigate(Href, replace: true);
    }

    private async ValueTask DisposeJsHandleAsync()
    {
        if (_handle is not null)
        {
            try { await _handle.InvokeVoidAsync("dispose").ConfigureAwait(false); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }

            try { await _handle.DisposeAsync().ConfigureAwait(false); }
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

        await DisposeJsHandleAsync().ConfigureAwait(false);

        if (_module is not null)
        {
            try { await _module.DisposeAsync().ConfigureAwait(false); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
            _module = null;
        }
    }
}
