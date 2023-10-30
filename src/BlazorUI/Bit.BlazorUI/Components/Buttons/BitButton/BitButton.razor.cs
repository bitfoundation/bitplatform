using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitButton
{
    private static readonly Dictionary<BitButtonStyle, string> StyleClassMorphemeMapping = new()
    {
        { BitButtonStyle.Primary, "pri"},
        { BitButtonStyle.Standard, "std"},
        { BitButtonStyle.Text, "txt"}
    };
    private static readonly Dictionary<BitButtonColor, string> ColorClassMorphemeMapping = new()
    {
        { BitButtonColor.None, String.Empty},
        { BitButtonColor.Info, "-info"},
        { BitButtonColor.Warning, "-warning"},
        { BitButtonColor.Success, "-success"},
        { BitButtonColor.Error, "-error"},
        { BitButtonColor.SevereWarning, "-severe-warning"},
    };
    
    
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private BitButtonColor buttonColor = BitButtonColor.None;

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
            buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The color of button
    /// </summary>
    [Parameter]
    public BitButtonColor ButtonColor
    {
        get => buttonColor;
        set
        {
            buttonColor = value;
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
    /// URL the link points to, if provided, button renders as an anchor
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Callback for when the button clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

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
        string styleClassMorpheme = StyleClassMorphemeMapping[buttonStyle];
        string colorClassMorpheme= ColorClassMorphemeMapping[buttonColor];
        
        ClassBuilder.Register(() => $"{RootElementClass}-{styleClassMorpheme}{colorClassMorpheme}");
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
