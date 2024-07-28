namespace Bit.BlazorUI;

public partial class BitAccordion : BitComponentBase
{
    /// <summary>
    /// Custom CSS classes for different parts of the BitAccordion.
    /// </summary>
    [Parameter] public BitAccordionClassStyles? Classes { get; set; }

    /// <summary>
    /// The content of the Accordion.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Default value of the IsExpanded.
    /// </summary>
    [Parameter] public bool? DefaultIsExpanded { get; set; }

    /// <summary>
    /// A short description in the header of Accordion.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Used to customize how the header inside the Accordion is rendered.
    /// </summary>
    [Parameter] public RenderFragment<bool>? HeaderTemplate { get; set; }

    /// <summary>
    /// Determines whether the accordion is expanding or collapses.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Callback that is called when the header is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback that is called when the IsExpanded value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitAccordion.
    /// </summary>
    [Parameter] public BitAccordionClassStyles? Styles { get; set; }

    /// <summary>
    /// Title in the header of Accordion.
    /// </summary>
    [Parameter] public string? Title { get; set; }


    protected override string RootElementClass => "bit-acd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsExpanded ? Classes?.Expanded : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsExpanded ? Styles?.Expanded : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        if (IsExpandedHasBeenSet is false && DefaultIsExpanded.HasValue)
        {
            await AssignIsExpanded(DefaultIsExpanded.Value);
        }

        await base.OnInitializedAsync();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        if (await AssignIsExpanded(IsExpanded is false) is false) return;

        await OnChange.InvokeAsync(IsExpanded);
    }
}
