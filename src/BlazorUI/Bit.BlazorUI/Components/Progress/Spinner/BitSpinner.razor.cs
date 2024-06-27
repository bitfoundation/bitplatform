namespace Bit.BlazorUI;

public partial class BitSpinner
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
    [Parameter] public BitSpinnerSize Size { get; set; }

    /// <summary>
    /// The label to show next to the spinner. Label updates will be announced to the screen readers
    /// </summary>
    [Parameter] public string? Label { get; set; }

    protected override string RootElementClass => "bit-spn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Size switch
        {
            BitSpinnerSize.XSmall => $"{RootElementClass}-xs",
            BitSpinnerSize.Small => $"{RootElementClass}-sm",
            BitSpinnerSize.Medium => $"{RootElementClass}-md",
            BitSpinnerSize.Large => $"{RootElementClass}-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => $"{RootElementClass}-top",
            BitLabelPosition.Start => $"{RootElementClass}-srt",
            BitLabelPosition.End => $"{RootElementClass}-end",
            BitLabelPosition.Bottom => $"{RootElementClass}-btm",
            _ => string.Empty
        });
    }
}
