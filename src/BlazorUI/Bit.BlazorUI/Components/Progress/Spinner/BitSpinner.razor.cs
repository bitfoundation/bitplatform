namespace Bit.BlazorUI;

public partial class BitSpinner : BitComponentBase
{
    /// <summary>
    /// Politeness setting for label update announcement.
    /// </summary>
    [Parameter] public BitSpinnerAriaLive AriaLive { get; set; } = BitSpinnerAriaLive.Polite;

    /// <summary>
    /// The position of the label in regards to the spinner animation
    /// </summary>
    [Parameter] public BitLabelPosition LabelPosition { get; set; }

    /// <summary>
    /// The size of spinner to render
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// The label to show next to the spinner. Label updates will be announced to the screen readers
    /// </summary>
    [Parameter] public string? Label { get; set; }



    protected override string RootElementClass => "bit-spn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-spn-sm",
            BitSize.Medium => "bit-spn-md",
            BitSize.Large => "bit-spn-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => "bit-spn-top",
            BitLabelPosition.Start => "bit-spn-srt",
            BitLabelPosition.End => "bit-spn-end",
            BitLabelPosition.Bottom => "bit-spn-btm",
            _ => string.Empty
        });
    }
}
