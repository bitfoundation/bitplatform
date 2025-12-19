namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCard"/> component.
/// </summary>
public class BitCardParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitCard cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCard value in
    /// parameterized APIs or configuration settings.
    /// <br />
    /// Using this constant helps ensure consistency and reduces the risk of typographical errors.
    /// </remarks>
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



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCard"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCard"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCard"/> will be updated. 
    /// This method does not overwrite existing values on <paramref name="bitCard"/>.
    /// </remarks>
    /// <param name="bitCard">
    /// The <see cref="BitCard"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCard bitCard)
    {
        if (bitCard is null) return;

        UpdateBaseParameters(bitCard);

        if (Background.HasValue && bitCard.HasNotBeenSet(nameof(Background)))
        {
            bitCard.Background = Background.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Border.HasValue && bitCard.HasNotBeenSet(nameof(Border)))
        {
            bitCard.Border = Border.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (FullHeight.HasValue && bitCard.HasNotBeenSet(nameof(FullHeight)))
        {
            bitCard.FullHeight = FullHeight.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (FullSize.HasValue && bitCard.HasNotBeenSet(nameof(FullSize)))
        {
            bitCard.FullSize = FullSize.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitCard.HasNotBeenSet(nameof(FullWidth)))
        {
            bitCard.FullWidth = FullWidth.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (NoShadow.HasValue && bitCard.HasNotBeenSet(nameof(NoShadow)))
        {
            bitCard.NoShadow = NoShadow.Value;

            bitCard.ClassBuilder.Reset();
        }
    }
}
