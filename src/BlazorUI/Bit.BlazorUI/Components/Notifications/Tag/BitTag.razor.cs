namespace Bit.BlazorUI;

/// <summary>
/// Tag component provides a visual representation of an attribute, person, or asset.
/// </summary>
public partial class BitTag : BitComponentBase
{
    /// <summary>
    /// Child content of component, the content that the tag will apply to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the tag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The icon to show inside the tag.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Click event handler of the tag.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Dismiss button click event, if set the dismiss icon will show up.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// The size of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the tag.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The visual variant of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-tag";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-tag-pri",
            BitColor.Secondary => "bit-tag-sec",
            BitColor.Tertiary => "bit-tag-ter",
            BitColor.Info => "bit-tag-inf",
            BitColor.Success => "bit-tag-suc",
            BitColor.Warning => "bit-tag-wrn",
            BitColor.SevereWarning => "bit-tag-swr",
            BitColor.Error => "bit-tag-err",
            _ => "bit-tag-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tag-sm",
            BitSize.Medium => "bit-tag-md",
            BitSize.Large => "bit-tag-lg",
            _ => "bit-tag-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-tag-fil",
            BitVariant.Outline => "bit-tag-otl",
            BitVariant.Text => "bit-tag-txt",
            _ => "bit-tag-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    private async Task HandleOnDismissClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnDismiss.InvokeAsync(e);
        }
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
