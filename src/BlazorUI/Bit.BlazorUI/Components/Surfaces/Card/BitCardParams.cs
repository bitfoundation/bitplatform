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
    /// Sets the shadow elevation level of the card (1-24).
    /// </summary>
    public int? Elevation { get; set; }

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
    /// Sets the height of the card explicitly.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Removes the default padding of the card.
    /// </summary>
    public bool? NoPadding { get; set; }

    /// <summary>
    /// Removes the default shadow around the card.
    /// </summary>
    public bool? NoShadow { get; set; }

    /// <summary>
    /// Renders the card with no shadow and a primary border.
    /// </summary>
    public bool? Outlined { get; set; }

    /// <summary>
    /// Removes the border-radius from the card, rendering it with sharp corners.
    /// </summary>
    public bool? Square { get; set; }

    /// <summary>
    /// Sets the width of the card explicitly.
    /// </summary>
    public string? Width { get; set; }



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

        if (Elevation.HasValue && bitCard.HasNotBeenSet(nameof(Elevation)))
        {
            bitCard.Elevation = Elevation.Value;

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

        if (NoPadding.HasValue && bitCard.HasNotBeenSet(nameof(NoPadding)))
        {
            bitCard.NoPadding = NoPadding.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (NoShadow.HasValue && bitCard.HasNotBeenSet(nameof(NoShadow)))
        {
            bitCard.NoShadow = NoShadow.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Outlined.HasValue && bitCard.HasNotBeenSet(nameof(Outlined)))
        {
            bitCard.Outlined = Outlined.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Square.HasValue && bitCard.HasNotBeenSet(nameof(Square)))
        {
            bitCard.Square = Square.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Height is not null && bitCard.HasNotBeenSet(nameof(Height)))
        {
            bitCard.Height = Height;

            bitCard.StyleBuilder.Reset();
        }

        if (Width is not null && bitCard.HasNotBeenSet(nameof(Width)))
        {
            bitCard.Width = Width;

            bitCard.StyleBuilder.Reset();
        }
    }
}
