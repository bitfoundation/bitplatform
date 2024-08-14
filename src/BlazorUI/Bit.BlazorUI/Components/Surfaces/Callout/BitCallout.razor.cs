namespace Bit.BlazorUI;

public partial class BitCallout : BitComponentBase, IDisposable
{
    private bool _disposed;
    private string _contentId = default!;
    private string _anchorId = default!;
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

        ClassBuilder.Register(() => IsOpen ? "bit-clo-opn" : string.Empty);
        ClassBuilder.Register(() => IsOpen ? Classes?.Opened : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsOpen ? Styles?.Opened : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _contentId = $"BitCallout-{UniqueId}-content";
        _anchorId = $"BitCallout-{UniqueId}-anchor";

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }
    }



    private async Task OpenCallout()
    {
        if (await AssignIsOpen(true) is false) return;

        await ToggleCallout();
    }

    private async Task CloseCallout()
    {
        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        var id = Anchor is not null ? _anchorId : AnchorId ?? _Id;

        await _js.ToggleCallout(_dotnetObj,
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
                                true,
                                RootElementClass);

        await OnToggle.InvokeAsync(IsOpen);
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            await _js.ClearCallout(_contentId);
            _dotnetObj.Dispose();
        }

        _disposed = true;
    }
}
