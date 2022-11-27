using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitLoadingButton
{
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private BitButtonSize buttonSize = BitButtonSize.Medium;
    private int? _tabIndex;

    /// <summary>
    /// Whether the icon button can have focus in disabled mode.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the icon button for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The type of the button.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The style of button, Possible values: Primary | Standard.
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
    /// The size of button, Possible values: Small | Medium | Large.
    /// </summary>
    [Parameter]
    public BitButtonSize ButtonSize
    {
        get => buttonSize;
        set
        {
            buttonSize = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The content of button, It can be Any custom tag or a text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    /// <summary>
    /// Determine whether the button is in loading mode or not.
    /// </summary>        
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// The loading label to show next to the spinner.
    /// </summary>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The size of loading spinner to render.
    /// </summary>
    [Parameter] public BitSpinnerSize LoadingSpinnerSize { get; set; } = BitSpinnerSize.Small;

    /// <summary>
    /// The position of the loading Label in regards to the spinner animation.
    /// </summary>
    [Parameter] public BitLabelPosition LoadingLabelPosition { get; set; } = BitLabelPosition.Right;

    /// <summary>
    /// Used to customize the content inside the Button in the Loading state.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Callback for when the button clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the icon button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    protected override string RootElementClass => "bit-lbtn";

    protected override async Task OnInitializedAsync()
    {
        if (IsEnabled is false)
        {
            _tabIndex = AllowDisabledFocus ? null : -1;
        }

        ButtonType ??= EditContext is null ? BitButtonType.Button : BitButtonType.Submit;

        await base.OnInitializedAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                                       ? string.Empty
                                       : ButtonStyle == BitButtonStyle.Primary
                                           ? "primary"
                                           : "standard");

        ClassBuilder.Register(() => ButtonSize == BitButtonSize.Small
                               ? $"{RootElementClass}-sm-{VisualClassRegistrar()}"
                               : ButtonSize == BitButtonSize.Medium
                                   ? $"{RootElementClass}-md-{VisualClassRegistrar()}"
                                   : $"{RootElementClass}-lg-{VisualClassRegistrar()}");
    }

    private string GetClassLoadingSize()
    {
        string classSize = LoadingSpinnerSize switch
        {
            BitSpinnerSize.XSmall => "xSmall",
            BitSpinnerSize.Small => "small",
            BitSpinnerSize.Medium => "medium",
            BitSpinnerSize.Large => "large",
            _ => "small"
        };

        return classSize;
    }

    private string GetClassLoadingLabelPosition()
    {
        string classLabelPosition = LoadingLabelPosition switch
        {
            BitLabelPosition.Top => "top",
            BitLabelPosition.Right => "right",
            BitLabelPosition.Bottom => "bottom",
            BitLabelPosition.Left => "left",
            _ => "right"
        };

        return classLabelPosition;
    }

    private string GetClassLoadingStyle()
    {
        string classLoadingStyle = buttonStyle switch
        {
            BitButtonStyle.Primary => "primary",
            BitButtonStyle.Standard => "standard",
            _ => "primary"
        };

        return classLoadingStyle;
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
