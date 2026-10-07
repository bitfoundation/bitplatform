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
    /// Stands the body of the card in with a placeholder while its content is being fetched.
    /// </summary>
    public bool? Loading { get; set; }

    /// <summary>
    /// The custom placeholder rendered in the body of the card while Loading is set.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

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

        if (Background.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Background), Background.Value, static c => c.Background, static (c, v) => c.Background = v);
        }

        if (Border.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Border), Border.Value, static c => c.Border, static (c, v) => c.Border = v);
        }

        if (Classes is not null)
        {
            bitCard.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Color.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Color), Color.Value, static c => c.Color, static (c, v) => c.Color = v);
        }

        if (CoverOverlay.HasValue)
        {
            bitCard.TakeFromCascade(nameof(CoverOverlay), CoverOverlay.Value, static c => c.CoverOverlay, static (c, v) => c.CoverOverlay = v);
        }

        if (Divider.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Divider), Divider.Value, static c => c.Divider, static (c, v) => c.Divider = v);
        }

        if (Elevation.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Elevation), Elevation.Value, static c => c.Elevation, static (c, v) => c.Elevation = v);
        }

        if (FullHeight.HasValue)
        {
            bitCard.TakeFromCascade(nameof(FullHeight), FullHeight.Value, static c => c.FullHeight, static (c, v) => c.FullHeight = v);
        }

        if (FullSize.HasValue)
        {
            bitCard.TakeFromCascade(nameof(FullSize), FullSize.Value, static c => c.FullSize, static (c, v) => c.FullSize = v);
        }

        if (FullWidth.HasValue)
        {
            bitCard.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static c => c.FullWidth, static (c, v) => c.FullWidth = v);
        }

        if (HeadingLevel.HasValue)
        {
            bitCard.TakeFromCascade(nameof(HeadingLevel), HeadingLevel.Value, static c => c.HeadingLevel, static (c, v) => c.HeadingLevel = v);
        }

        if (Reversed.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Reversed), Reversed.Value, static c => c.Reversed, static (c, v) => c.Reversed = v);
        }

        if (Horizontal.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Horizontal), Horizontal.Value, static c => c.Horizontal, static (c, v) => c.Horizontal = v);
        }

        if (Hoverable.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Hoverable), Hoverable.Value, static c => c.Hoverable, static (c, v) => c.Hoverable = v);
        }

        if (ImageLoading.HasValue)
        {
            bitCard.TakeFromCascade(nameof(ImageLoading), ImageLoading.Value, static c => c.ImageLoading, static (c, v) => c.ImageLoading = v);
        }

        if (Download is not null)
        {
            bitCard.TakeFromCascade(nameof(Download), Download, static c => c.Download, static (c, v) => c.Download = v);
        }

        if (Loading.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Loading), Loading.Value, static c => c.Loading, static (c, v) => c.Loading = v);
        }

        if (LoadingTemplate is not null)
        {
            bitCard.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static c => c.LoadingTemplate, static (c, v) => c.LoadingTemplate = v);
        }

        if (NoPadding.HasValue)
        {
            bitCard.TakeFromCascade(nameof(NoPadding), NoPadding.Value, static c => c.NoPadding, static (c, v) => c.NoPadding = v);
        }

        if (NoShadow.HasValue)
        {
            bitCard.TakeFromCascade(nameof(NoShadow), NoShadow.Value, static c => c.NoShadow, static (c, v) => c.NoShadow = v);
        }

        if (Outlined.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Outlined), Outlined.Value, static c => c.Outlined, static (c, v) => c.Outlined = v);
        }

        if (Rel.HasValue && bitCard.TakeFromCascade(nameof(Rel), Rel.Value, static c => c.Rel, static (c, v) => c.Rel = v))
        {
            bitCard.OnSetHrefAndRel();
        }

        if (ScrollableBody.HasValue)
        {
            bitCard.TakeFromCascade(nameof(ScrollableBody), ScrollableBody.Value, static c => c.ScrollableBody, static (c, v) => c.ScrollableBody = v);
        }

        if (Size.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (Square.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Square), Square.Value, static c => c.Square, static (c, v) => c.Square = v);
        }

        if (StopPropagation.HasValue)
        {
            bitCard.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static c => c.StopPropagation, static (c, v) => c.StopPropagation = v);
        }

        if (Variant.HasValue)
        {
            bitCard.TakeFromCascade(nameof(Variant), Variant.Value, static c => c.Variant, static (c, v) => c.Variant = v);
        }

        if (Target is not null && bitCard.TakeFromCascade(nameof(Target), Target, static c => c.Target, static (c, v) => c.Target = v))
        {
            bitCard.OnSetHrefAndRel();
        }

        if (Height is not null)
        {
            bitCard.TakeFromCascade(nameof(Height), Height, static c => c.Height, static (c, v) => c.Height = v);
        }

        if (CoverRatio is not null)
        {
            bitCard.TakeFromCascade(nameof(CoverRatio), CoverRatio, static c => c.CoverRatio, static (c, v) => c.CoverRatio = v);
        }

        if (CoverWidth is not null)
        {
            bitCard.TakeFromCascade(nameof(CoverWidth), CoverWidth, static c => c.CoverWidth, static (c, v) => c.CoverWidth = v);
        }

        if (ImageHeight is not null)
        {
            bitCard.TakeFromCascade(nameof(ImageHeight), ImageHeight, static c => c.ImageHeight, static (c, v) => c.ImageHeight = v);
        }

        if (ImagePosition is not null)
        {
            bitCard.TakeFromCascade(nameof(ImagePosition), ImagePosition, static c => c.ImagePosition, static (c, v) => c.ImagePosition = v);
        }

        if (MaxHeight is not null)
        {
            bitCard.TakeFromCascade(nameof(MaxHeight), MaxHeight, static c => c.MaxHeight, static (c, v) => c.MaxHeight = v);
        }

        if (MaxWidth is not null)
        {
            bitCard.TakeFromCascade(nameof(MaxWidth), MaxWidth, static c => c.MaxWidth, static (c, v) => c.MaxWidth = v);
        }

        if (MinHeight is not null)
        {
            bitCard.TakeFromCascade(nameof(MinHeight), MinHeight, static c => c.MinHeight, static (c, v) => c.MinHeight = v);
        }

        if (MinWidth is not null)
        {
            bitCard.TakeFromCascade(nameof(MinWidth), MinWidth, static c => c.MinWidth, static (c, v) => c.MinWidth = v);
        }

        if (Styles is not null)
        {
            bitCard.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (Width is not null)
        {
            bitCard.TakeFromCascade(nameof(Width), Width, static c => c.Width, static (c, v) => c.Width = v);
        }
    }
}
