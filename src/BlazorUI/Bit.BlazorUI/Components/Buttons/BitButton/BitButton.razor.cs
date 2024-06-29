using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitButton
{
    private BitSize? size;
    private BitColor? color;
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private BitButtonIconPosition? iconPosition = BitButtonIconPosition.Start;

    private int? _tabIndex;
    private BitButtonType _buttonType;


    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }


    /// <summary>
    /// Whether the button can have focus in disabled mode
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The style of button, Possible values: Primary | Standard | Text
    /// </summary>
    [Parameter]
    public BitButtonStyle ButtonStyle
    {
        get => buttonStyle;
        set
        {
            if (buttonStyle == value) return;

            buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The type of the button
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content of button, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitButton.
    /// </summary>
    [Parameter] public BitButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of button
    /// </summary>
    [Parameter]
    public BitColor? Color
    {
        get => color;
        set
        {
            if (color == value) return;

            color = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// URL the link points to, if provided, button renders as an anchor
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// The icon to show inside the BitButton.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Specifies Icon position which can be rendered either at the start or end of the component.
    /// </summary>
    [Parameter]
    public BitButtonIconPosition? IconPosition
    {
        get => iconPosition;
        set
        {
            if (iconPosition == value) return;

            iconPosition = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Determine whether the button is in loading mode or not.
    /// </summary>        
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>
    /// The loading label to show next to the spinner.
    /// </summary>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The position of the loading Label in regards to the spinner animation.
    /// </summary>
    [Parameter] public BitLabelPosition LoadingLabelPosition { get; set; } = BitLabelPosition.End;

    /// <summary>
    /// Used to customize the content inside the Button in the Loading state.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Callback for when the button clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

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
    /// Custom CSS styles for different parts of the BitButton.
    /// </summary>
    [Parameter] public BitButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the button
    /// </summary>
    [Parameter] public string? Title { get; set; }


    protected override string RootElementClass => "bit-btn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ButtonStyle switch
        {
            BitButtonStyle.Primary => "bit-btn-pri",
            BitButtonStyle.Standard => "bit-btn-std",
            BitButtonStyle.Text => "bit-btn-txt",
            _ => "bit-btn-pri"
        });
        
        ClassBuilder.Register(() => Color switch
        {
            BitColor.Info => "bit-btn-inf",
            BitColor.Success => "bit-btn-suc",
            BitColor.Warning => "bit-btn-wrn",
            BitColor.SevereWarning => "bit-btn-swr",
            BitColor.Error => "bit-btn-err",
            _ => string.Empty
        });
        
        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-btn-sm",
            BitSize.Medium => "bit-btn-md",
            BitSize.Large => "bit-btn-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => IconPosition switch
        {
            BitButtonIconPosition.Start => "bit-btn-srt",
            BitButtonIconPosition.End => "bit-btn-end",
            _ => "bit-btn-srt"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        if (IsEnabled is false)
        {
            _tabIndex = AllowDisabledFocus ? null : -1;
        }

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        base.OnParametersSet();
    }
        
    private string GetLabelPositionClass()
        => LoadingLabelPosition switch
        {
            BitLabelPosition.Top => "bit-btn-top",
            BitLabelPosition.Start => "bit-btn-srt",
            BitLabelPosition.End => "bit-btn-end",
            BitLabelPosition.Bottom => "bit-btn-btm",
            _ => "bit-btn-end"
        };

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
