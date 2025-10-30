namespace Bit.BlazorUI;

/// <summary>
/// A callout is an anchored tip that can be used to teach people or guide them through the app without blocking them.
/// </summary>
public partial class BitCallout : BitComponentBase
{
    private string _anchorId = default!;
    private string _contentId = default!;
    private DotNetObjectReference<BitCallout> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the anchor element of the callout.
    /// </summary>
    [Parameter] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The setter function for element reference to the external anchor element.
    /// </summary>
    [Parameter] public Func<ElementReference>? AnchorEl { get; set; }

    /// <summary>
    /// The id of the external anchor element.
    /// </summary>
    [Parameter] public string? AnchorId { get; set; }

    /// <summary>
    /// The content of the callout.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the callout.
    /// </summary>
    [Parameter] public BitCalloutClassStyles? Classes { get; set; }

    /// <summary>
    /// Alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// Determines the allowed directions in which the callout should decide to be opened.
    /// </summary>
    [Parameter] public BitDropDirection? Direction { get; set; }

    /// <summary>
    /// Forces the callout to preserve its component's original width.
    /// </summary>
    [Parameter] public bool FixedCalloutWidth { get; set; }

    /// <summary>
    /// The id of the footer element that renders at the end of the scrolling container of the callout contnet.
    /// </summary>
    [Parameter] public string? FooterId { get; set; }

    /// <summary>
    /// The id of the header element that renders at the top of the scrolling container of the callout contnet.
    /// </summary>
    [Parameter] public string? HeaderId { get; set; }

    /// <summary>
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetIsOpen))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The max window width to consider when calculating the position of the callout before openning.
    /// </summary>
    [Parameter] public int? MaxWindowWidth { get; set; }

    /// <summary>
    /// The callback that is called when the callout opens or closes.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

    /// <summary>
    /// Forces the callout to set its content container width while opening based on the available space and actual content.
    /// </summary>
    [Parameter] public bool SetCalloutWidth { get; set; }

    /// <summary>
    /// The id of the element which needs to be scrollable in the content of the callout.
    /// </summary>
    [Parameter] public string? ScrollContainerId { get; set; }

    /// <summary>
    /// The vertical offset of the scroll container to consider in the positining and height calculation of the callout.
    /// </summary>
    [Parameter] public int? ScrollOffset { get; set; }

    /// <summary>
    /// Configures the responsive mode of the callout for the small screens.
    /// </summary>
    [Parameter] public BitResponsiveMode? ResponsiveMode { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the callout.
    /// </summary>
    [Parameter] public BitCalloutClassStyles? Styles { get; set; }



    /// <summary>
    /// Toggles the callout to open/close it.
    /// </summary>
    /// <returns></returns>
    public async Task Toggle()
    {
        if (IsEnabled is false) return;
        if (await AssignIsOpen(IsOpen is false) is false) return;

        await ToggleCallout();
    }

    [JSInvokable("CloseCallout")]
    public async Task CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-clo";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsOpen ? Classes?.Opened : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsOpen ? Styles?.Opened : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _anchorId = $"BitCallout-{UniqueId}-anchor";
        _contentId = $"BitCallout-{UniqueId}-content";

        await base.OnInitializedAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        base.OnAfterRender(firstRender);
    }



    private async Task OpenCallout()
    {
        if (IsEnabled is false) return;
        if (await AssignIsOpen(true) is false) return;

        await ToggleCallout();
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        var id = Anchor is not null ? _anchorId : AnchorId ?? _Id;

        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: id,
            component: AnchorEl is null ? null : AnchorEl(),
            calloutId: _contentId,
            callout: null,
            isCalloutOpen: IsOpen,
            responsiveMode: ResponsiveMode ?? BitResponsiveMode.None,
            dropDirection: Direction ?? BitDropDirection.TopAndBottom,
            isRtl: Dir is BitDir.Rtl,
            scrollContainerId: ScrollContainerId ?? "",
            scrollOffset: ScrollOffset ?? 0,
            headerId: HeaderId ?? "",
            footerId: FooterId ?? "",
            setCalloutWidth: SetCalloutWidth,
            fixedCalloutWidth: FixedCalloutWidth,
            maxWindowWidth: MaxWindowWidth ?? 0);

        await OnToggle.InvokeAsync(IsOpen);
    }

    private void OnSetIsOpen()
    {
        _ = ToggleCallout();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitCalloutClearCallout(_contentId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
