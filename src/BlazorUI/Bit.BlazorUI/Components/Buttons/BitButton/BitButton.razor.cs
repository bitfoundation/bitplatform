using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitButton : BitComponentBase
{
    private string? _rel;
    private int? _tabIndex;
    private BitButtonType _buttonType;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>.
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }



    /// <summary>
    /// Whether the button can have focus in disabled mode.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an aria-hidden attribute instructing screen readers to ignore the element.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// If true, shows the loading state while the OnClick event is in progress.
    /// </summary>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// The value of the type attribute of the button.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content of primary section of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the button.
    /// </summary>
    [Parameter] public BitButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Preserves the foreground color of the button through hover and focus.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FixedColor { get; set; }

    /// <summary>
    /// Apply floating behavior.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Float { get; set; }

    /// <summary>
    /// Apply position absolute when the button is in floating mode.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FloatAbsolute { get; set; }

    /// <summary>
    /// The position of the button in floating mode.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPosition? FloatPosition { get; set; }

    /// <summary>
    /// Expand the button width to 100% of the available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The value of the href attribute of the link rendered by the button. If provided, the component will be rendered as an anchor tag instead of button.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Href { get; set; }

    /// <summary>
    /// The name of the icon to render inside the button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered.
    /// </summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>
    /// Determines whether the button is in loading mode or not.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsLoading { get; set; }

    /// <summary>
    /// The loading label text to show next to the spinner icon.
    /// </summary>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The position of the loading Label in regards to the spinner icon.
    /// </summary>
    [Parameter] public BitLabelPosition LoadingLabelPosition { get; set; } = BitLabelPosition.End;

    /// <summary>
    /// The custom template used to replace the default loading text inside the button in the loading state.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The callback for the click event of the button.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The content of the primary section of the button (alias of the ChildContent).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? PrimaryTemplate { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the main content of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReversedIcon { get; set; }

    /// <summary>
    /// If Href provided, specifies the relationship between the current document and the linked document.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRel? Rel { get; set; }

    /// <summary>
    /// The text of the secondary section of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? SecondaryText { get; set; }

    /// <summary>
    /// The custom template for the secondary section of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? SecondaryTemplate { get; set; }

    /// <summary>
    /// The size of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the button.
    /// </summary>
    [Parameter] public BitButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Specifies target attribute of the link when the button renders as an anchor (by providing the Href parameter).
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-btn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ((PrimaryTemplate ?? ChildContent) is null &&
                                    SecondaryText.HasNoValue() && SecondaryTemplate is null) ||
                                    IconOnly
                                        ? "bit-btn-ntx"
                                        : string.Empty);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-btn-fil",
            BitVariant.Outline => "bit-btn-otl",
            BitVariant.Text => "bit-btn-txt",
            _ => "bit-btn-fil"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-btn-pri",
            BitColor.Secondary => "bit-btn-sec",
            BitColor.Tertiary => "bit-btn-ter",
            BitColor.Info => "bit-btn-inf",
            BitColor.Success => "bit-btn-suc",
            BitColor.Warning => "bit-btn-wrn",
            BitColor.SevereWarning => "bit-btn-swr",
            BitColor.Error => "bit-btn-err",
            BitColor.PrimaryBackground => "bit-btn-pbg",
            BitColor.SecondaryBackground => "bit-btn-sbg",
            BitColor.TertiaryBackground => "bit-btn-tbg",
            BitColor.PrimaryForeground => "bit-btn-pfg",
            BitColor.SecondaryForeground => "bit-btn-sfg",
            BitColor.TertiaryForeground => "bit-btn-tfg",
            BitColor.PrimaryBorder => "bit-btn-pbr",
            BitColor.SecondaryBorder => "bit-btn-sbr",
            BitColor.TertiaryBorder => "bit-btn-tbr",
            _ => "bit-btn-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-btn-sm",
            BitSize.Medium => "bit-btn-md",
            BitSize.Large => "bit-btn-lg",
            _ => "bit-btn-md"
        });

        ClassBuilder.Register(() => ReversedIcon ? "bit-btn-rvi" : string.Empty);

        ClassBuilder.Register(() => FixedColor ? "bit-btn-ftc" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-btn-flw" : string.Empty);

        ClassBuilder.Register(() => Float && FloatAbsolute ? "bit-btn-fab" :  
                                    Float ? "bit-btn-flt" : string.Empty);

        ClassBuilder.Register(() => Float ? FloatPosition switch
        {
            BitPosition.TopRight => "bit-btn-trg",
            BitPosition.TopCenter => "bit-btn-tcr",
            BitPosition.TopLeft => "bit-btn-tlf",
            BitPosition.CenterLeft => "bit-btn-clf",
            BitPosition.BottomLeft => "bit-btn-blf",
            BitPosition.BottomCenter => "bit-btn-bcr",
            BitPosition.BottomRight => "bit-btn-brg",
            BitPosition.CenterRight => "bit-btn-crg",
            BitPosition.Center => "bit-btn-ctr",
            _ => "bit-btn-brg"
        } : string.Empty);
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

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (AutoLoading)
        {
            if (await AssignIsLoading(true) is false) return;
        }

        await OnClick.InvokeAsync(e);

        await AssignIsLoading(false);
    }

    private void OnSetHrefAndRel()
    {
        if (Rel.HasValue is false || Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        _rel = BitLinkRelUtils.GetRels(Rel.Value);
    }
}
