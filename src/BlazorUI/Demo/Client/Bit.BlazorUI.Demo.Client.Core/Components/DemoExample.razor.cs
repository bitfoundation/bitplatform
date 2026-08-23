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
    private DotNetObjectReference<DemoExample>? _dotnetObj;

    [AutoInject] private DemoContentDeferral _contentDeferral = default!;

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
    /// Whether this example may hold its preview back until the reader approaches it.
    /// <para>
    /// Beyond <see cref="DemoContentDeferral"/> - which is what decides that this is a page built on
    /// the client rather than the prerendered one - two things have to hold. There has to be an Id,
    /// since that is what the observer is given to watch. And the address must not carry a fragment:
    /// the browser is then about to jump to an anchor, and an anchor is only in the right place once
    /// everything above it is the size it will end up being.
    /// </para>
    /// </summary>
    private bool ShouldDeferPreview => RenderForMcpClient is false
                                    && _previewElementId is not null
                                    && InPrerenderSession is false
                                    && _contentDeferral.IsEnabled
                                    && NavigationManager.Uri.Contains('#', StringComparison.Ordinal) is false;

    protected override Task OnInitAsync()
    {
        // The MCP branch prints the source as markdown rather than rendering the panel, so nothing
        // there is mounted or highlighted; showCode only matters for the browser branch below.
        showCode = RenderForMcpClient;

        _isPreviewMounted = ShouldDeferPreview is false;

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
    public Task OnPreviewReached()
    {
        if (_isPreviewMounted) return Task.CompletedTask;

        _isPreviewMounted = true;

        // The observer has already stopped watching and will not report again, so the reference it
        // was holding has nothing left to reach.
        _dotnetObj?.Dispose();
        _dotnetObj = null;

        return InvokeAsync(StateHasChanged);
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
