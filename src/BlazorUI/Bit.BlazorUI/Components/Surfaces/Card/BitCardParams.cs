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
    /// Custom CSS classes for different parts of the card.
    /// </summary>
    public BitCardClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the card.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Lays the cover of the card behind its content instead of above it, filling the whole surface.
    /// </summary>
    public bool? CoverOverlay { get; set; }

    /// <summary>
    /// The aspect ratio the cover of the card is drawn at, as a CSS ratio such as 16 / 9.
    /// </summary>
    public string? CoverRatio { get; set; }

    /// <summary>
    /// The width of the cover of a horizontal card.
    /// </summary>
    public string? CoverWidth { get; set; }

    /// <summary>
    /// Draws a hairline between the header and the body of the card and between its body and its footer.
    /// </summary>
    public bool? Divider { get; set; }

    /// <summary>
    /// The download attribute of the stretched link of the card.
    /// </summary>
    public string? Download { get; set; }

    /// <summary>
    /// Sets the shadow elevation level of the card (0-24).
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
    /// The heading level the title of the card reports itself as (1-6).
    /// </summary>
    public int? HeadingLevel { get; set; }

    /// <summary>
    /// Sets the height of the card explicitly.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Lays the cover of the card beside its content instead of above it.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Lifts the card while the pointer is over it.
    /// </summary>
    public bool? Hoverable { get; set; }

    /// <summary>
    /// The height of the cover image of the card.
    /// </summary>
    public string? ImageHeight { get; set; }

    /// <summary>
    /// The loading behavior of the cover image of the card, eager or lazy.
    /// </summary>
    public BitImageLoading? ImageLoading { get; set; }

    /// <summary>
    /// The part of the cover image of the card that is kept in frame, as a CSS object-position such as top or 50% 20%.
    /// </summary>
    public string? ImagePosition { get; set; }

    /// <summary>
    /// Sets the maximum height of the card.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Sets the maximum width of the card.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Sets the minimum height of the card.
    /// </summary>
    public string? MinHeight { get; set; }

    /// <summary>
    /// Sets the minimum width of the card.
    /// </summary>
    public string? MinWidth { get; set; }

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
    /// The rel attribute of the stretched link of the card.
    /// </summary>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Lays the cover of the card after its content instead of before it.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// Lets the content of the card scroll inside it instead of growing past the height it was given.
    /// </summary>
    public bool? ScrollableBody { get; set; }

    /// <summary>
    /// The size of the card, which sets its padding, the gap between its parts and the type of its header.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Removes the border-radius from the card, rendering it with sharp corners.
    /// </summary>
    public bool? Square { get; set; }

    /// <summary>
    /// Stops the propagation of the click event of the card.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the card.
    /// </summary>
    public BitCardClassStyles? Styles { get; set; }

    /// <summary>
    /// The target attribute of the stretched link of the card.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The visual variant of the card, which only takes effect while a Color is set.
    /// </summary>
    public BitVariant? Variant { get; set; }

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

        if (Classes is not null && bitCard.HasNotBeenSet(nameof(Classes)))
        {
            bitCard.Classes = Classes;

            bitCard.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitCard.HasNotBeenSet(nameof(Color)))
        {
            bitCard.Color = Color.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (CoverOverlay.HasValue && bitCard.HasNotBeenSet(nameof(CoverOverlay)))
        {
            bitCard.CoverOverlay = CoverOverlay.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Divider.HasValue && bitCard.HasNotBeenSet(nameof(Divider)))
        {
            bitCard.Divider = Divider.Value;

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

        if (HeadingLevel.HasValue && bitCard.HasNotBeenSet(nameof(HeadingLevel)))
        {
            bitCard.HeadingLevel = HeadingLevel.Value;
        }

        if (Reversed.HasValue && bitCard.HasNotBeenSet(nameof(Reversed)))
        {
            bitCard.Reversed = Reversed.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Horizontal.HasValue && bitCard.HasNotBeenSet(nameof(Horizontal)))
        {
            bitCard.Horizontal = Horizontal.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Hoverable.HasValue && bitCard.HasNotBeenSet(nameof(Hoverable)))
        {
            bitCard.Hoverable = Hoverable.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (ImageLoading.HasValue && bitCard.HasNotBeenSet(nameof(ImageLoading)))
        {
            bitCard.ImageLoading = ImageLoading.Value;
        }

        if (Download is not null && bitCard.HasNotBeenSet(nameof(Download)))
        {
            bitCard.Download = Download;
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

        if (Rel.HasValue && bitCard.HasNotBeenSet(nameof(Rel)))
        {
            bitCard.Rel = Rel.Value;

            bitCard.OnSetHrefAndRel();
        }

        if (ScrollableBody.HasValue && bitCard.HasNotBeenSet(nameof(ScrollableBody)))
        {
            bitCard.ScrollableBody = ScrollableBody.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitCard.HasNotBeenSet(nameof(Size)))
        {
            bitCard.Size = Size.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Square.HasValue && bitCard.HasNotBeenSet(nameof(Square)))
        {
            bitCard.Square = Square.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitCard.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitCard.StopPropagation = StopPropagation.Value;
        }

        if (Variant.HasValue && bitCard.HasNotBeenSet(nameof(Variant)))
        {
            bitCard.Variant = Variant.Value;

            bitCard.ClassBuilder.Reset();
        }

        if (Target is not null && bitCard.HasNotBeenSet(nameof(Target)))
        {
            bitCard.Target = Target;

            bitCard.OnSetHrefAndRel();
        }

        if (Height is not null && bitCard.HasNotBeenSet(nameof(Height)))
        {
            bitCard.Height = Height;

            bitCard.StyleBuilder.Reset();
        }

        if (CoverRatio is not null && bitCard.HasNotBeenSet(nameof(CoverRatio)))
        {
            bitCard.CoverRatio = CoverRatio;

            bitCard.ClassBuilder.Reset();
            bitCard.StyleBuilder.Reset();
        }

        if (CoverWidth is not null && bitCard.HasNotBeenSet(nameof(CoverWidth)))
        {
            bitCard.CoverWidth = CoverWidth;

            bitCard.StyleBuilder.Reset();
        }

        if (ImageHeight is not null && bitCard.HasNotBeenSet(nameof(ImageHeight)))
        {
            bitCard.ImageHeight = ImageHeight;

            bitCard.StyleBuilder.Reset();
        }

        if (ImagePosition is not null && bitCard.HasNotBeenSet(nameof(ImagePosition)))
        {
            bitCard.ImagePosition = ImagePosition;

            bitCard.StyleBuilder.Reset();
        }

        if (MaxHeight is not null && bitCard.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitCard.MaxHeight = MaxHeight;

            bitCard.StyleBuilder.Reset();
        }

        if (MaxWidth is not null && bitCard.HasNotBeenSet(nameof(MaxWidth)))
        {
            bitCard.MaxWidth = MaxWidth;

            bitCard.StyleBuilder.Reset();
        }

        if (MinHeight is not null && bitCard.HasNotBeenSet(nameof(MinHeight)))
        {
            bitCard.MinHeight = MinHeight;

            bitCard.StyleBuilder.Reset();
        }

        if (MinWidth is not null && bitCard.HasNotBeenSet(nameof(MinWidth)))
        {
            bitCard.MinWidth = MinWidth;

            bitCard.StyleBuilder.Reset();
        }

        if (Styles is not null && bitCard.HasNotBeenSet(nameof(Styles)))
        {
            bitCard.Styles = Styles;

            bitCard.StyleBuilder.Reset();
        }

        if (Width is not null && bitCard.HasNotBeenSet(nameof(Width)))
        {
            bitCard.Width = Width;

            bitCard.StyleBuilder.Reset();
        }
    }
}
