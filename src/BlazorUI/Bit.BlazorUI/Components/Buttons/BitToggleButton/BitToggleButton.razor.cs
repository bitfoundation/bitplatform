namespace Bit.BlazorUI;

public partial class BitToggleButton
{
    private bool IsCheckedHasBeenSet;

    private BitSize? size;
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
    /// The content of BitToggleButton.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitToggleButton component.
    /// </summary>
    [Parameter] public BitToggleButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// Default value of the IsChecked.
    /// </summary>
    [Parameter] public bool? DefaultIsChecked { get; set; }

    /// <summary>
    /// The icon that shows in the button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

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
    /// Callback that is called when the IsChecked value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Callback that is called when the button is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The icon of the BitToggleButton when it is not checked.
    /// </summary>
    [Parameter] public string? OffIconName { get; set; }

    /// <summary>
    /// The text of the BitToggleButton when it is not checked.
    /// </summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>
    /// The title of the BitToggleButton when it is not checked.
    /// </summary>
    [Parameter] public string? OffTitle { get; set; }

    /// <summary>
    /// The icon of the BitToggleButton when it is checked.
    /// </summary>
    [Parameter] public string? OnIconName { get; set; }

    /// <summary>
    /// The text of the BitToggleButton when it is checked.
    /// </summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>
    /// The title of the BitToggleButton when it is checked.
    /// </summary>
    [Parameter] public string? OnTitle { get; set; }

    /// <summary>
    /// The size of button, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter]
    public BitSize? Size
    {
        get => size;
        set
        {
            if (size == value) return;

            size = value;

            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom CSS styles for different parts of the BitToggleButton component.
    /// </summary>
    [Parameter] public BitToggleButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the BitToggleButton.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the button.
    /// </summary>
    [Parameter] public string? Title { get; set; }


    protected override string RootElementClass => "bit-tgb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsChecked ? $"bit-tgb-chk {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => ButtonStyle switch
        {
            BitButtonStyle.Primary => "bit-tgb-pri",
            BitButtonStyle.Standard => "bit-tgb-std",
            BitButtonStyle.Text => "bit-tgb-txt",
            _ => "bit-tgb-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tgb-sm",
            BitSize.Medium => "bit-tgb-md",
            BitSize.Large => "bit-tgb-lg",
            _ => string.Empty
        });
    }
    
    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsChecked ? Styles?.Checked : string.Empty);
    }

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

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        if (IsCheckedHasBeenSet && IsCheckedChanged.HasDelegate is false) return;

        IsChecked = !IsChecked;
        await OnChange.InvokeAsync(IsChecked);
    }


    private string? GetIconName()
    {
        if (IsChecked && OnIconName.HasValue()) return OnIconName;

        if (IsChecked is false && OffIconName.HasValue()) return OffIconName;

        return IconName;
    }

    private string? GetText()
    {
        if (IsChecked && OnText.HasValue()) return OnText;

        if (IsChecked is false && OffText.HasValue()) return OffText;

        return Text;
    }

    private string? GetTitle()
    {
        if (IsChecked && OnTitle.HasValue()) return OnTitle;

        if (IsChecked is false && OffTitle.HasValue()) return OffTitle;

        return Title;
    }
}
