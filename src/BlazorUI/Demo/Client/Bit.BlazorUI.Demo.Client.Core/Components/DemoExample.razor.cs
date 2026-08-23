namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoExample
{
    private bool showCode;

    // The code panel is mounted on the first open and stays mounted from then on (see the razor),
    // so a page never pays - in its prerendered HTML, in its render tree, or in a Prism pass - for
    // the tens of KB of sample source behind panels the reader never opens.
    private bool _isCodeMounted;
    private bool _isCodeHighlighted;

    // The same treatment for the live preview, except that this one the reader never asks for: it is
    // mounted as they approach it and stays mounted from then on. Starts true - so nothing is held
    // back - unless this render is one where holding back is safe; see ShouldDeferPreview.
    private bool _isPreviewMounted = true;

    // The room a held-back preview keeps, in pixels, when this page is the prerendered one: the
    // height its own prerendered copy was measured at. Zero everywhere else, where the placeholder
    // falls back to the one height the stylesheet gives it.
    private int _pendingPreviewHeight;

    private DotNetObjectReference<DemoExample>? _dotnetObj;

    [AutoInject] private DemoContentDeferral _contentDeferral = default!;

    /// <summary>
    /// The page this example belongs to, which is what keeps its held-back preview in line to be
    /// filled in during the browser's idle time. Null for a DemoExample used outside a DemoPage, in
    /// which case the visibility observer is the only thing that ever mounts it.
    /// </summary>
    [CascadingParameter] public DemoPage? Page { get; set; }

    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public string Id { get; set; } = default!;
    [Parameter] public string RazorCode { get; set; } = default!;
    [Parameter] public string CsharpCode { get; set; } = default!;
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    [Parameter] public bool PreventRenderForMcpClient { get; set; }

    // A panel the reader has opened stays rendered so it can animate shut, which would otherwise turn
    // the page-wide highlight pass below into O(examples²) work. Highlighting only this example's own
    // container keeps it linear. Falls back to null - and so to the whole document, exactly as
    // before - for the rare DemoExample declared without an Id.
    private string? _codeElementId => Id.HasValue() ? $"{Id}-code" : null;

    /// <summary>The element the visibility observer watches, which is the preview's own container.</summary>
    private string? _previewElementId => Id.HasValue() ? $"{Id}-preview" : null;

    /// <summary>
    /// Whether this example may hold its preview back until the reader approaches it - and, when it
    /// may, how much room the placeholder has to keep.
    /// <para>
    /// Four things always have to hold. This must not be the MCP rendering or a prerender, both of
    /// which owe their reader the whole page in one pass. There has to be an Id, since that is what
    /// the observer is given to watch. And the address must not carry a fragment: the browser is then
    /// about to jump to an anchor, and an anchor is only in the right place once everything above it
    /// is the size it will end up being.
    /// </para>
    /// <para>
    /// The fourth is that the page must still be building itself. Holding a preview back is only free
    /// while the reader has nowhere to stand - during a navigation, at the top of a page that is not
    /// there yet. An example that renders into a page already on screen is one a BitPivot tab has just
    /// brought in on a multi-API page (Dropdown, Button, ...), and the reader is standing in the middle
    /// of it: each preview mounted above them then grows the page above the scroll position and slides
    /// everything else down under their eyes, one example and one render at a time. A tab the reader
    /// asked for is therefore built in the one pass that swaps it in - the page moves once, where it
    /// would otherwise drift for as long as the fill-in lasts.
    /// </para>
    /// <para>
    /// What is left is the difference between a page built from nothing on the client - where holding
    /// back is free, and <see cref="DemoContentDeferral"/> says so - and the first page, whose
    /// prerendered copy the reader is looking at while this decision is being made. There, holding
    /// back is only free if the placeholder stands exactly where the markup it replaces stood, so the
    /// prerendered preview is measured first and the answer is no unless that measurement worked.
    /// </para>
    /// </summary>
    private bool ShouldDeferPreview()
    {
        if (RenderForMcpClient) return false;
        if (InPrerenderSession) return false;
        if (_previewElementId is null) return false;
        if (NavigationManager.Uri.Contains('#', StringComparison.Ordinal)) return false;
        if (Page?.HasRendered is true) return false;

        // A page this app built, or a hybrid app that never prerenders anything: there is no copy of
        // this preview on screen for a placeholder to contradict.
        if (_contentDeferral.IsEnabled || AppRenderMode.IsBlazorHybrid) return true;

        // The first page. The render batch this decision belongs to has not reached the DOM yet, so
        // what is still there to measure is the prerendered preview itself. Under Blazor Server this
        // cannot be asked synchronously and comes back as 0, which keeps the first page exactly as it
        // was.
        _pendingPreviewHeight = (int)Math.Ceiling(JSRuntime.TryGetElementHeight(_previewElementId));

        return _pendingPreviewHeight > 0;
    }

    protected override Task OnInitAsync()
    {
        // The MCP branch prints the source as markdown rather than rendering the panel, so nothing
        // there is mounted or highlighted; showCode only matters for the browser branch below.
        showCode = RenderForMcpClient;

        _isPreviewMounted = ShouldDeferPreview() is false;

        return Task.CompletedTask;
    }

    // Only once, and only after the reader has opened the panel: the code comes from constant
    // fields and stays mounted once shown, so there is never a second thing to highlight.
    // Re-running it on every render made any interaction inside any example re-tokenize the code of
    // every example on the page, since the state change re-renders the whole demo and with it all
    // of its DemoExample children.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (RenderForMcpClient) return;

        if (firstRender && _isPreviewMounted is false)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            await JSRuntime.ObserveVisibility(_previewElementId!, _dotnetObj, nameof(OnPreviewReached));

            // And, failing that, in the browser's own time: the observer only ever answers for what
            // the reader approaches, and a page that stays half-built is one where a deep link lands
            // in the wrong place and find-in-page misses half the words. Registered here rather than
            // in OnInit so the queue is in document order - a child's OnAfterRender runs before its
            // parent's, and the page starts the backfill from its own.
            Page?.QueueBackfill(MountPreviewAsync);
        }

        if (_isCodeMounted is false || _isCodeHighlighted) return;

        _isCodeHighlighted = true;

        await JSRuntime.InvokeVoid("highlightSnippet", _codeElementId);
    }



    private void ToggleCode()
    {
        showCode = !showCode;

        // Mounting on the way open only: the way shut has to keep the panel's content in the DOM
        // for BitCollapse to animate it away.
        _isCodeMounted = _isCodeMounted || showCode;
    }



    private string AppendCodePhraseToCsharpCode()
    {
        string code = $@"{"\n\n"}@code {{
{CsharpCode.Trim().Replace("\n", "\n\t")}
}}";
        return code;
    }

    // Both copy buttons confirm by swapping their icon for a checkmark and growing into an inline
    // "copied" label; the title doubles as the accessible name for the same confirmation.
    private bool isCodeCopied = false;
    private string codeIcon = BitIconName.Copy;
    private string copyCodeMessage = "Copy code";
    private async Task CopyCodeToClipboard()
    {
        var code = string.IsNullOrEmpty(CsharpCode) is false
                    ? AppendCodePhraseToCsharpCode()
                    : "";

        await JSRuntime.CopyToClipboard(RazorCode.Trim() + code);

        codeIcon = BitIconName.CheckMark;
        copyCodeMessage = "Code copied!";
        isCodeCopied = true;

        StateHasChanged();

        await Task.Delay(1000);
        isCodeCopied = false;
        codeIcon = BitIconName.Copy;
        copyCodeMessage = "Copy code";

        StateHasChanged();
    }

    private bool isLinkCopied = false;
    private string linkIcon = BitIconName.Link;
    private string copyLinkMessage = "Copy link";
    private async Task CopyLinkToClipboard()
    {
        var currentUrl = NavigationManager.Uri;
        currentUrl = currentUrl.Contains("#") ? currentUrl.Substring(0, currentUrl.IndexOf("#")) : currentUrl;
        var exampleUrl = $"{currentUrl}#{Id}";
        await JSRuntime.CopyToClipboard(exampleUrl);

        linkIcon = BitIconName.CheckMark;
        copyLinkMessage = "Link copied!";
        isLinkCopied = true;

        StateHasChanged();

        await Task.Delay(1000);
        isLinkCopied = false;
        linkIcon = BitIconName.Link;
        copyLinkMessage = "Copy link";

        StateHasChanged();
    }



    /// <summary>
    /// The reader has come within reach of this example. Mounting is one way: the preview stays from
    /// here on, so scrolling back up never rebuilds what is already there.
    /// </summary>
    [JSInvokable]
    public async Task OnPreviewReached()
    {
        // The observer stops watching before it reports, so there is nothing left to unregister.
        await MountPreviewAsync(stillObserved: false);
    }

    /// <summary>
    /// Builds the live preview during the browser's idle time, and says whether it had anything left
    /// to build - the reader may well have scrolled past it first, in which case the page's backfill
    /// moves straight on to the next one rather than spending an idle slice on nothing.
    /// </summary>
    private Task<bool> MountPreviewAsync() => MountPreviewAsync(stillObserved: true);

    private async Task<bool> MountPreviewAsync(bool stillObserved)
    {
        if (_isPreviewMounted) return false;

        _isPreviewMounted = true;

        // Only the backfill leaves a live observer behind.
        if (stillObserved)
        {
            try
            {
                await JSRuntime.UnobserveVisibility(_previewElementId!);
            }
            catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unregister
        }

        // The observer has nothing left to report, so the reference it was holding has nothing left
        // to reach.
        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await InvokeAsync(StateHasChanged);

        return true;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing && _dotnetObj is not null)
        {
            try
            {
                await JSRuntime.UnobserveVisibility(_previewElementId!);
            }
            catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unregister

            _dotnetObj.Dispose();
            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }
}
