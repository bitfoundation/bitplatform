namespace Bit.BlazorUI;

public partial class BitDropMenu : BitComponentBase
{
    private bool _disposed;
    private string _calloutId = default!;
    private DotNetObjectReference<BitDropMenu> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The icon name of the chevron down part of the drop menu.
    /// </summary>
    [Parameter] public string? ChevronDownIcon { get; set; }

    /// <summary>
    /// The content of the callout of the drop menu.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the drop menu.
    /// </summary>
    [Parameter] public BitDropMenuClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon to show inside the header of the drop menu.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines the opening state of the callout of the drop menu.
    /// </summary>
    [Parameter, CallOnSet(nameof(ToggleCallout))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The callback is called when the drop menu is clicked.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the drop menu.
    /// </summary>
    [Parameter] public BitDropMenuClassStyles? Styles { get; set; }

    /// <summary>
    /// The custom content to render inside the header of the drop menu.
    /// </summary>
    [Parameter] public RenderFragment? Template { get; set; }

    /// <summary>
    /// The text to show inside the header of the drop menu.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Makes the background of the header of the drop menu transparent.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Transparent { get; set; }


    [JSInvokable("CloseCallout")]
    public async Task CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-drm";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsOpen ? Classes?.Opened : string.Empty);

        ClassBuilder.Register(() => Transparent ? "bit-drm-trn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsOpen ? Styles?.Opened : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _calloutId = $"BitDropMenu-{UniqueId}-callout";

        await base.OnInitializedAsync();
    }



    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;

        await OpenCallout();

        await OnClick.InvokeAsync();
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

        await _js.ToggleCallout(_dotnetObj,
                                _Id,
                                null,
                                _calloutId,
                                null,
                                IsOpen,
                                BitResponsiveMode.None,
                                BitDropDirection.TopAndBottom,
                                Dir is BitDir.Rtl,
                                "",
                                0,
                                "",
                                "",
                                false,
                                RootElementClass);
    }
}
