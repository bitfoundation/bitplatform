namespace Bit.BlazorUI;

public partial class BitCallout : BitComponentBase, IDisposable
{
    private bool _disposed;
    private string _calloutId = default!;
    private string _anchorId = default!;
    private DotNetObjectReference<BitCallout> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// The content of the anchor section.
    /// </summary>
    [Parameter] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The element reference to the anchor element.
    /// </summary>
    [Parameter] public ElementReference? AnchorElement { get; set; }

    /// <summary>
    /// The id of the anchor element.
    /// </summary>
    [Parameter] public string? AnchorId { get; set; }

    /// <summary>
    /// The content of the menu button, that are BitMenuButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the menu button.
    /// </summary>
    [Parameter] public BitCalloutClassStyles? Classes { get; set; }

    /// <summary>
    /// Alias for ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(ToggleCallout))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The callback is called when the menu button header is clicked.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// The callback that is called when the callout opens or closes.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the menu button.
    /// </summary>
    [Parameter] public BitCalloutClassStyles? Styles { get; set; }



    [JSInvokable("CloseCallout")]
    public async Task CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    public async Task Toggle()
    {
        if (await AssignIsOpen(IsOpen is false) is false) return;

        await ToggleCallout();
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
        _calloutId = $"BitCallout-{UniqueId}-callout";
        _anchorId = $"BitCallout-{UniqueId}-anchor";

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
                                _calloutId,
                                IsOpen,
                                BitResponsiveMode.None,
                                BitDropDirection.TopAndBottom,
                                Dir is BitDir.Rtl,
                                "",
                                0,
                                "",
                                "",
                                true,
                                RootElementClass);
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
            await _js.ClearCallout(_calloutId);
            _dotnetObj.Dispose();
        }

        _disposed = true;
    }
}
