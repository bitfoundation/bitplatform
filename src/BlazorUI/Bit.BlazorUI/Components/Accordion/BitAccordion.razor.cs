
namespace Bit.BlazorUI;

public partial class BitAccordion
{
    protected override bool UseVisual => false;

    private bool IsExpandedHasBeenSet;
    private bool isExpanded;

    /// <summary>
    /// Default value of the IsExpanded.
    /// </summary>
    [Parameter] public bool? DefaultIsExpanded { get; set; }

    /// <summary>
    /// The content of the Accordion.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

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
    [Parameter]
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (value == isExpanded) return;
            isExpanded = value;
            _ = IsExpandedChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsExpandedChanged { get; set; }

    /// <summary>
    /// Callback that is called when the header is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback that is called when the IsExpanded value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Title in the header of Accordion.
    /// </summary>
    [Parameter] public string? Title { get; set; }


    protected override string RootElementClass => "bit-acd";

    protected override async Task OnInitializedAsync()
    {
        if (IsExpandedHasBeenSet is false && DefaultIsExpanded.HasValue)
        {
            IsExpanded = DefaultIsExpanded.Value;
        }

        await base.OnInitializedAsync();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
        if (IsExpandedHasBeenSet && IsExpandedChanged.HasDelegate is false) return;

        IsExpanded = !IsExpanded;
        await OnChange.InvokeAsync(IsExpanded);
    }
}
