namespace Bit.BlazorUI;

/// <summary>
/// A callout is an anchored tip that can be used to teach people or guide them through the app without blocking them.
/// </summary>
public partial class BitCallout : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;
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
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(ToggleCallout))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The callback that is called when the callout opens or closes.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

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
        _dotnetObj = DotNetObjectReference.Create(this);

        _anchorId = $"BitCallout-{UniqueId}-anchor";
        _contentId = $"BitCallout-{UniqueId}-content";

        await base.OnInitializedAsync();
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
        if (IsEnabled is false) return;

        var id = Anchor is not null ? _anchorId : AnchorId ?? _Id;

        await _js.BitCalloutToggleCallout(_dotnetObj,
                                id,
                                AnchorEl is null ? null : AnchorEl(),
                                _contentId,
                                null,
                                IsOpen,
                                ResponsiveMode ?? BitResponsiveMode.None,
                                Direction ?? BitDropDirection.TopAndBottom,
                                Dir is BitDir.Rtl,
                                "",
                                0,
                                "",
                                "",
                                false);

        await OnToggle.InvokeAsync(IsOpen);
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitCalloutClearCallout(_contentId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _disposed = true;
    }
}
