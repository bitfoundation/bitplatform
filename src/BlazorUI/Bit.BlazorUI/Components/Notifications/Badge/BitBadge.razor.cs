namespace Bit.BlazorUI;

/// <summary>
/// Badge component is a small visual element used to highlight or indicate specific information within a user interface.
/// </summary>
public partial class BitBadge : BitComponentBase
{
    private string? _content;



    /// <summary>
    /// Child content of component, the content that the badge will apply to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitBadge.
    /// </summary>
    [Parameter] public BitBadgeClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the badge.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Content you want inside the badge. Supported types are string and integer.
    /// </summary>
    [Parameter] 
    [CallOnSet(nameof(OnSetContentAndMax))]
    public object? Content { get; set; }

    /// <summary>
    /// Reduces the size of the badge and hide any of its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Dot { get; set; }

    /// <summary>
    /// The visibility of the badge.
    /// </summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>
    /// Sets the Icon to use in the badge.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Max value to display when content is integer type.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetContentAndMax))]
    public int? Max { get; set; }

    /// <summary>
    /// Button click event if set.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Overlaps the badge on top of the child content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Overlap { get; set; }

    /// <summary>
    /// The position of the badge.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPosition? Position { get; set; }

    /// <summary>
    /// The size of badge, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitBadge.
    /// </summary>
    [Parameter] public BitBadgeClassStyles? Styles { get; set; }

    /// <summary>
    /// The visual variant of the badge.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-bdg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-bdg-pri",
            BitColor.Secondary => "bit-bdg-sec",
            BitColor.Tertiary => "bit-bdg-ter",
            BitColor.Info => "bit-bdg-inf",
            BitColor.Success => "bit-bdg-suc",
            BitColor.Warning => "bit-bdg-wrn",
            BitColor.SevereWarning => "bit-bdg-swr",
            BitColor.Error => "bit-bdg-err",
            _ => "bit-bdg-pri"
        });

        ClassBuilder.Register(() => Dot ? "bit-bdg-dot" : string.Empty);

        ClassBuilder.Register(() => Overlap ? "bit-bdg-orp" : string.Empty);

        ClassBuilder.Register(() => Position switch
        {
            BitPosition.TopRight => "bit-bdg-trg",
            BitPosition.TopCenter => "bit-bdg-tcr",
            BitPosition.TopLeft => "bit-bdg-tlf",
            BitPosition.CenterLeft => "bit-bdg-clf",
            BitPosition.BottomLeft => "bit-bdg-blf",
            BitPosition.BottomCenter => "bit-bdg-bcr",
            BitPosition.BottomRight => "bit-bdg-brg",
            BitPosition.CenterRight => "bit-bdg-crg",
            BitPosition.Center => "bit-bdg-ctr",
            _ => "bit-bdg-trg"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-bdg-sm",
            BitSize.Medium => "bit-bdg-md",
            BitSize.Large => "bit-bdg-lg",
            _ => "bit-bdg-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-bdg-fil",
            BitVariant.Outline => "bit-bdg-otl",
            BitVariant.Text => "bit-bdg-txt",
            _ => "bit-bdg-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private void OnSetContentAndMax()
    {
        if (Content is string stringContent)
        {
            _content = stringContent;
        }
        else if (Content is int numberContent)
        {
            if (Max.HasValue && numberContent > Max)
            {
                _content = Max + "+";
            }
            else
            {
                _content = numberContent.ToString();
            }
        }
        else
        {
            _content = null;
        }
    }
}
