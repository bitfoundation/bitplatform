using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

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
public sealed class BrouterLink : ComponentBase, IDisposable
{
    [Inject] private IBrouter Brouter { get; set; } = default!;
    [Inject] private BrouterOptions Options { get; set; } = default!;

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
    /// If true, navigation replaces the current history entry instead of adding a new one.
    /// Modified clicks (Ctrl/Cmd+click, Shift+click, middle-click) still defer to native
    /// browser behavior such as opening in a new tab.
    /// </summary>
    [Parameter] public bool Replace { get; set; }


    private bool _isActive;
    private bool _shouldPreventDefault;

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
        var target = NormalisePath(Href);
        var comparison = Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        _isActive = Match switch
        {
            BrouterLinkMatch.All => string.Equals(current, target, comparison),
            _ => current.StartsWith(target, comparison) &&
                 (current.Length == target.Length || target == "/" || current[target.Length] == '/' || current[target.Length] == '?' || current[target.Length] == '#')
        };
    }

    private static string NormalisePath(string href)
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
        if (path.Length > 1 && path.EndsWith('/')) path = path[..^1];
        if (path.Length == 0 || path[0] != '/') path = "/" + path;
        return path;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var combinedClass = string.IsNullOrEmpty(Class)
            ? (_isActive ? ActiveClass : null)
            : (_isActive ? $"{Class} {ActiveClass}".Trim() : Class);

        builder.OpenElement(0, "a");
        if (AdditionalAttributes is not null) builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "href", Href);
        if (combinedClass is not null) builder.AddAttribute(3, "class", combinedClass);
        if (_isActive) builder.AddAttribute(4, "aria-current", "page");
        // By default, rely on Blazor's NavigationInterception (same as Microsoft's NavLink) to drive
        // navigation off the anchor's href. The Replace parameter cannot be expressed via a plain
        // anchor, so opt into a custom click handler with preventDefault only in that case.
        // We use onmousedown to detect whether the upcoming click is an unmodified primary click
        // and only then set preventDefault/stopPropagation to true, preserving native modified-click
        // behavior (Ctrl/Cmd+click opens in new tab, etc.).
        if (Replace)
        {
            builder.AddAttribute(5, "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseDown));
            builder.AddAttribute(6, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
            builder.AddAttribute(7, "onclick:preventDefault", _shouldPreventDefault);
            builder.AddAttribute(8, "onclick:stopPropagation", _shouldPreventDefault);
        }
        builder.AddContent(9, ChildContent);
        builder.CloseElement();
    }

    private void OnMouseDown(MouseEventArgs e)
    {
        // Set the flag only for unmodified primary clicks so that modified clicks
        // (Ctrl+click, Shift+click, middle-click) fall through to native browser behavior.
        _shouldPreventDefault = e.Button == 0 && !e.CtrlKey && !e.ShiftKey && !e.AltKey && !e.MetaKey;
    }

    private void OnClick(MouseEventArgs e)
    {
        // Only invoked when Replace=true (see BuildRenderTree).
        // Navigate only when the mousedown indicated an unmodified primary click.
        if (!_shouldPreventDefault) return;
        _shouldPreventDefault = false;

        Brouter.Navigate(Href, replace: Replace);
    }

    public void Dispose()
    {
        Brouter.OnNavigated -= OnNavigated;
    }
}
