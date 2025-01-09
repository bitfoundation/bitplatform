namespace Bit.BlazorUI;

/// <summary>
/// ToggleButton is a type of button that stores and shows a status representing the toggle state of the component.
/// </summary>
public partial class BitToggleButton : BitComponentBase
{
    private int? _tabIndex;



    /// <summary>
    /// Whether the toggle button can have focus in disabled mode.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; }

    /// <summary>
    /// Detailed description of the toggle button for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The content of the toggle button.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the toggle button.
    /// </summary>
    [Parameter] public BitToggleButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the IsChecked parameter.
    /// </summary>
    [Parameter] public bool? DefaultIsChecked { get; set; }

    /// <summary>
    /// The icon name that renders inside the toggle button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines if the toggle button is in the checked state.
    /// </summary>        
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsChecked { get; set; }

    /// <summary>
    /// Callback for when the IsChecked value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Callback for when the toggle button is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The icon of the toggle button when it is not checked.
    /// </summary>
    [Parameter] public string? OffIconName { get; set; }

    /// <summary>
    /// The text of the toggle button when it is not checked.
    /// </summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>
    /// The title of the toggle button when it is not checked.
    /// </summary>
    [Parameter] public string? OffTitle { get; set; }

    /// <summary>
    /// The icon of the toggle button when it is checked.
    /// </summary>
    [Parameter] public string? OnIconName { get; set; }

    /// <summary>
    /// The text of the toggle button when it is checked.
    /// </summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>
    /// The title of the toggle button when it is checked.
    /// </summary>
    [Parameter] public string? OnTitle { get; set; }

    /// <summary>
    /// The size of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the toggle button.
    /// </summary>
    [Parameter] public BitToggleButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the toggle button.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the toggle button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-tgb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsChecked ? $"bit-tgb-chk {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-tgb-pri",
            BitColor.Secondary => "bit-tgb-sec",
            BitColor.Tertiary => "bit-tgb-ter",
            BitColor.Info => "bit-tgb-inf",
            BitColor.Success => "bit-tgb-suc",
            BitColor.Warning => "bit-tgb-wrn",
            BitColor.SevereWarning => "bit-tgb-swr",
            BitColor.Error => "bit-tgb-err",
            BitColor.PrimaryBackground => "bit-tgb-pbg",
            BitColor.SecondaryBackground => "bit-tgb-sbg",
            BitColor.TertiaryBackground => "bit-tgb-tbg",
            BitColor.PrimaryForeground => "bit-tgb-pfg",
            BitColor.SecondaryForeground => "bit-tgb-sfg",
            BitColor.TertiaryForeground => "bit-tgb-tfg",
            BitColor.PrimaryBorder => "bit-tgb-pbr",
            BitColor.SecondaryBorder => "bit-tgb-sbr",
            BitColor.TertiaryBorder => "bit-tgb-tbr",
            _ => "bit-tgb-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tgb-sm",
            BitSize.Medium => "bit-tgb-md",
            BitSize.Large => "bit-tgb-lg",
            _ => "bit-tgb-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-tgb-fil",
            BitVariant.Outline => "bit-tgb-otl",
            BitVariant.Text => "bit-tgb-txt",
            _ => "bit-tgb-fil"
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
            await AssignIsChecked(DefaultIsChecked.Value);
        }

        await base.OnInitializedAsync();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        if (await AssignIsChecked(IsChecked is false) is false) return;

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
