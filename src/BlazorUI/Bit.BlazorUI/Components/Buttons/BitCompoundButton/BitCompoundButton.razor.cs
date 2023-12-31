using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitCompoundButton
{
    private BitColor? color;
    private BitButtonSize? size;
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private BitButtonIconPosition? iconPosition = BitButtonIconPosition.Start;

    private int? _tabIndex;
    private BitButtonType _buttonType;


    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }


    /// <summary>
    /// Whether the BitCompoundButton can have focus in disabled mode.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the BitCompoundButton for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an aria-hidden attribute instructing screen readers to ignore the element.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The style of the BitCompoundButton.
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
    /// The value of the type attribute of the button rendered by the BitCompoundButton.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content of primary section of the BitCompoundButton.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCompoundButton.
    /// </summary>
    [Parameter] public BitCompoundButtonClassStyles? Classes { get; set; }

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
    /// The value of the href attribute of the link rendered by the BitCompoundButton. If provided, the component will be rendered as an anchor.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// The icon to show inside the BitCompoundButton.
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
    /// The callback for the click event of the BitCompoundButton.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The content of primary section of the BitCompoundButton (alias of the ChildContent).
    /// </summary>
    [Parameter] public RenderFragment? PrimaryTemplate { get; set; }

    /// <summary>
    /// The text of the secondary section of the BitCompoundButton.
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// The RenderFragment for the secondary section of the BitCompoundButton.
    /// </summary>
    [Parameter] public RenderFragment? SecondaryTemplate { get; set; }

    /// <summary>
    /// The size of button, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter]
    public BitButtonSize? Size
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
    /// Custom CSS styles for different parts of the BitCompoundButton.
    /// </summary>
    [Parameter] public BitCompoundButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Specifies target attribute of the link when the BitComponentButton renders as an anchor.
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button.
    /// </summary>
    [Parameter] public string? Title { get; set; }


    protected override string RootElementClass => "bit-cmb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ButtonStyle switch
        {
            BitButtonStyle.Primary => "bit-cmb-pri",
            BitButtonStyle.Standard => "bit-cmb-std",
            BitButtonStyle.Text => "bit-cmb-txt",
            _ => "bit-cmb-pri"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Info => "bit-cmb-inf",
            BitColor.Success => "bit-cmb-suc",
            BitColor.Warning => "bit-cmb-wrn",
            BitColor.SevereWarning => "bit-cmb-swr",
            BitColor.Error => "bit-cmb-err",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Size switch
        {
            BitButtonSize.Small => "bit-cmb-sm",
            BitButtonSize.Medium => "bit-cmb-md",
            BitButtonSize.Large => "bit-cmb-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => IconPosition switch
        {
            BitButtonIconPosition.Start => "bit-cmb-srt",
            BitButtonIconPosition.End => "bit-cmb-end",
            _ => "bit-cmb-srt"
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


    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
