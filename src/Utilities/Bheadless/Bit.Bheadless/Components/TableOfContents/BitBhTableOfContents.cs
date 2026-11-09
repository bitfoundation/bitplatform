using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// A headless table of contents with scroll spy (Ark TableOfContents): a <c>&lt;nav&gt;</c> of links to the sections of a
/// page whose current section - the first one in view, or every one in view with <see cref="Multiple"/> - gets
/// <c>aria-current="location"</c> and <c>data-active</c>. The browser watches the sections (an IntersectionObserver) and
/// .NET hears only when the current one changes.
///
/// <code>
/// &lt;BitBhTableOfContents Ids="@(["install", "usage", "api"])" class="toc"&gt;
///     &lt;BitBhTableOfContentsLink Target="install"&gt;Install&lt;/BitBhTableOfContentsLink&gt;
///     &lt;BitBhTableOfContentsLink Target="usage"&gt;Usage&lt;/BitBhTableOfContentsLink&gt;
///     &lt;BitBhTableOfContentsLink Target="api"&gt;API&lt;/BitBhTableOfContentsLink&gt;
/// &lt;/BitBhTableOfContents&gt;
/// </code>
/// </summary>
public class BitBhTableOfContents : BitBhComponentBase, IAsyncDisposable
{
    private readonly HashSet<string> _active = new(StringComparer.Ordinal);
    private DotNetObjectReference<Callbacks>? _ref;
    private int _handle;
    private bool _syncing;
    private bool _disposed;
    private string? _sentKey;

    /// <summary>The ids of the sections (headings) to watch, in page order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Ids { get; set; } = [];

    /// <summary>Marks every section in view as current, not only the first. Default: false.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    /// The IntersectionObserver root margin that decides "in view". Default: <c>"0px 0px -60% 0px"</c> - the top 40% of the
    /// viewport (a section counts once its heading is near the top). Make the top margin the height of a sticky header.
    /// </summary>
    [Parameter]
    public string? RootMargin { get; set; }

    /// <summary>Fires when the current sections change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> OnActiveChange { get; set; }

    /// <summary>The links (and any structure around them).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "nav";

    /// <summary>The ids of the current sections.</summary>
    public IReadOnlyCollection<string> ActiveIds => _active;

    internal bool IsActive(string id) => _active.Contains(id);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhTableOfContents>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", false);
        builder.AddComponentParameter(33, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (!Tag.Equals("nav", StringComparison.OrdinalIgnoreCase)) attrs["role"] = "navigation";
        attrs["aria-label"] = Strings.TableOfContents;
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Re-attached when the watched sections change (a page whose headings load later).
        if (_syncing || _disposed) return;
        _syncing = true;
        try
        {
            // Loops while the sections change during a round trip: the render that changed them returned early above.
            string key;
            while (!_disposed && (key = $"{string.Join('|', Ids)}#{Multiple}#{RootMargin}") != _sentKey)
            {
                _sentKey = key;
                if (_handle > 0) await Interop.ScrollSpyDetachAsync(_handle);
                _ref ??= DotNetObjectReference.Create(new Callbacks(this));
                _handle = await Interop.ScrollSpyAttachAsync(_ref, [.. Ids], new Dictionary<string, object?>
                {
                    ["multiple"] = Multiple,
                    ["rootMargin"] = RootMargin,
                });
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await Interop.ScrollSpyDetachAsync(_handle);
    }

    private sealed class Callbacks(BitBhTableOfContents owner)
    {
        /// <summary>The runtime calls it by this name with the ids of the sections now current.</summary>
        [JSInvokable]
        public Task OnScrollSpy(string[] ids) => owner.RunFromRuntimeAsync(() => owner.SetActiveAsync(ids));
    }

    internal async Task SetActiveAsync(IReadOnlyList<string> ids)
    {
        if (_active.SetEquals(ids)) return;
        _active.Clear();
        foreach (var id in ids) _active.Add(id);
        await OnActiveChange.InvokeAsync(ids);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (!_syncing && _handle > 0) await Interop.ScrollSpyDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// A link of a <see cref="BitBhTableOfContents"/> to the section <see cref="Target"/> on the current page; the current one has
/// <c>aria-current="location"</c> and <c>data-active</c>.
/// </summary>
public class BitBhTableOfContentsLink : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTableOfContents? Toc { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The id of the section the link goes to.</summary>
    [Parameter, EditorRequired]
    public string Target { get; set; } = string.Empty;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "a";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // The current page's address with the fragment: a bare "#target" resolves against <base href> - another page.
        if (Tag.Equals("a", StringComparison.OrdinalIgnoreCase))
        {
            var uri = Navigation.Uri;
            var hash = uri.IndexOf('#');
            attrs["href"] = $"{(hash >= 0 ? uri[..hash] : uri)}#{Target}";
        }
        var active = Toc?.IsActive(Target) ?? false;
        if (active) attrs["aria-current"] = "location";
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "active", active);
        return attrs;
    }
}
