namespace Bit.BlazorUI;

public class BitCardParams : IBitComponentParams
{
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCard)}";



    public string Name => ParamName;



    /// <summary>
    /// The color kind of the background of the card.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the card.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Makes the card height 100% of its parent container.
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes the card width and height 100% of its parent container.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Makes the card width 100% of its parent container.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Removes the default shadow around the card.
    /// </summary>
    public bool? NoShadow { get; set; }



    public void UpdateParameters(BitCard bitCard)
    {
        if (bitCard.CascadingParameters is null) return;

        if (bitCard.CascadingParameters.Background.HasValue && bitCard.HasNotBeenSet(nameof(Background)))
        {
            bitCard.Background = bitCard.CascadingParameters.Background.Value;
        }

        if (bitCard.CascadingParameters.Border.HasValue && bitCard.HasNotBeenSet(nameof(Border)))
        {
            bitCard.Border = bitCard.CascadingParameters.Border.Value;
        }

        if (bitCard.CascadingParameters.FullHeight.HasValue && bitCard.HasNotBeenSet(nameof(FullHeight)))
        {
            bitCard.FullHeight = bitCard.CascadingParameters.FullHeight.Value;
        }

        if (bitCard.CascadingParameters.FullSize.HasValue && bitCard.HasNotBeenSet(nameof(FullSize)))
        {
            bitCard.FullSize = bitCard.CascadingParameters.FullSize.Value;
        }

        if (bitCard.CascadingParameters.FullWidth.HasValue && bitCard.HasNotBeenSet(nameof(FullWidth)))
        {
            bitCard.FullWidth = bitCard.CascadingParameters.FullWidth.Value;
        }

        if (bitCard.CascadingParameters.NoShadow.HasValue && bitCard.HasNotBeenSet(nameof(NoShadow)))
        {
            bitCard.NoShadow = bitCard.CascadingParameters.NoShadow.Value;
        }
    }
}
