
namespace Bit.BlazorUI;

public partial class BitToggleButton
{
    protected override bool UseVisual => false;
    private bool IsCheckedHasBeenSet;
    private bool isChecked;
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private int? _tabIndex;
    
    /// <summary>
    /// Whether the toggle button can have focus in disabled mode.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the toggle button for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The style of compound button, Possible values: Primary | Standard.
    /// </summary>
    [Parameter] 
    public BitButtonStyle ButtonStyle
    {
        get => buttonStyle;
        set
        {
            buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Default value of the IsChecked.
    /// </summary>
    [Parameter] public bool? DefaultIsChecked { get; set; }

    /// <summary>
    /// URL the link points to, if provided, button renders as an anchor.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Determine if the button is in checked state, default is true.
    /// </summary>        
    [Parameter]
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (value == isChecked) return;
            isChecked = value;
            ClassBuilder.Reset();
            _ = IsCheckedChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    /// <summary>
    /// The icon that shows in the button.
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// The text that shows in the label.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Callback that is called when the button is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback that is called when the IsChecked value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link.
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    protected override string RootElementClass => "bit-tglb";

    protected override async Task OnInitializedAsync()
    {
        if (IsEnabled is false)
        {
            _tabIndex = AllowDisabledFocus ? null : -1;
        }

        if (IsCheckedHasBeenSet is false && DefaultIsChecked.HasValue)
        {
            IsChecked = DefaultIsChecked.Value;
        }

        await base.OnInitializedAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => ButtonStyle == BitButtonStyle.Primary
                                       ? "primary"
                                       : "standard");

        ClassBuilder.Register(() => IsChecked
                                       ? "checked" 
                                       : string.Empty);
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
            if (IsCheckedHasBeenSet && IsCheckedChanged.HasDelegate is false) return;
            IsChecked = !IsChecked;
            await OnChange.InvokeAsync(IsChecked);
        }
    }
}
