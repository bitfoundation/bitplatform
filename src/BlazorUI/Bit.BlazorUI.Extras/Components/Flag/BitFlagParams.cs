namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitFlag"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every flag of a list, a picker or a whole app agrees on - the size, the shape, the
/// image source, how the flag is named to assistive technologies - rather than what makes one flag the flag it is.
/// The country (Country, Iso2, Iso3, Code, Name), the Alt, the Title, the Src and the callbacks are deliberately not
/// here: they belong to a single flag, and cascading them would give every flag on the page the same one.
/// </remarks>
public class BitFlagParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitFlag"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitFlag value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitFlag)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets the aspect ratio of the frame of the flag, as any CSS aspect-ratio value.
    /// </summary>
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the flag is named to assistive technologies with the name of its country.
    /// </summary>
    public bool? AutoAlt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tooltip of the flag is the name of its country.
    /// </summary>
    public bool? AutoTitle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a hairline border is drawn around the flag.
    /// </summary>
    public bool? Bordered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the flag is clipped into a circle.
    /// </summary>
    public bool? Circular { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS classes for the different parts of the flag.
    /// </summary>
    public BitFlagClassStyles? Classes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the flag is rendered as its Unicode emoji instead of as an image.
    /// </summary>
    public bool? Emoji { get; set; }

    /// <summary>
    /// Gets or sets what to render in place of the flag when there is none to draw.
    /// </summary>
    public RenderFragment? FallbackTemplate { get; set; }

    /// <summary>
    /// Gets or sets how the flag image is scaled and cropped to fit the frame around it.
    /// </summary>
    public BitImageFit? Fit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the flag is drawn in shades of grey.
    /// </summary>
    public bool? Grayscale { get; set; }

    /// <summary>
    /// Gets or sets the height of the flag, as any CSS length.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Gets or sets the additional attributes rendered on the img element of the flag. They are merged with the
    /// flag's own, which win over them.
    /// </summary>
    public Dictionary<string, object>? ImageAttributes { get; set; }

    /// <summary>
    /// Gets or sets the image set of the Bit.BlazorUI.Assets package the flag is drawn out of.
    /// </summary>
    public BitFlagImageSet? ImageSet { get; set; }

    /// <summary>
    /// Gets or sets the size of the image of the <see cref="ImageSet"/> to draw.
    /// </summary>
    public BitFlagImageSize? ImageSize { get; set; }

    /// <summary>
    /// Gets or sets how the browser should load the flag image.
    /// </summary>
    public BitImageLoading? Loading { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the corners of the flag are rounded.
    /// </summary>
    public bool? Rounded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a shadow is drawn under the flag.
    /// </summary>
    public bool? Shadow { get; set; }

    /// <summary>
    /// Gets or sets the size of the flag, out of the icon sizes of the theme.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets the url pattern of the flag image of every country, which is what points every flag under the
    /// <see cref="BitParams"/> at one set of images of the page's own (e.g. "https://flagcdn.com/{iso2}.svg").
    /// </summary>
    public string? SrcPattern { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS styles for the different parts of the flag.
    /// </summary>
    public BitFlagClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the width of the flag, as any CSS length.
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitFlag"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitFlag"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitFlag"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitFlag"/>.
    /// </remarks>
    /// <param name="bitFlag">
    /// The <see cref="BitFlag"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitFlag bitFlag)
    {
        if (bitFlag is null) return;

        UpdateBaseParameters(bitFlag);

        // A value set on the flag itself wins over the cascade, and that has to hold between parameters as well as
        // within one: Circular wins over Rounded, Emoji over every image source, SrcPattern over ImageSet, a Width or
        // a Height over Size, and two lengths over AspectRatio. So a cascaded value that would outrank, or reshape,
        // what the flag set for itself stands down - and one applied on an earlier render, before the flag set its
        // own, is taken back off, which is why each of these is worked out as the value the flag should hold rather
        // than only as whether to assign it.
        var ownRounded = IsOwn(bitFlag, nameof(Rounded)) && bitFlag.Rounded;
        var ownImageSet = IsOwn(bitFlag, nameof(ImageSet)) && bitFlag.ImageSet.HasValue;
        var ownImage = ownImageSet
                    || (IsOwn(bitFlag, nameof(BitFlag.Src)) && bitFlag.Src.HasValue())
                    || (IsOwn(bitFlag, nameof(SrcPattern)) && bitFlag.SrcPattern.HasValue());
        var ownWidth = IsOwn(bitFlag, nameof(Width)) && bitFlag.Width.HasValue();
        var ownHeight = IsOwn(bitFlag, nameof(Height)) && bitFlag.Height.HasValue();
        var ownSize = ownWidth || ownHeight || (IsOwn(bitFlag, nameof(Size)) && bitFlag.Size.HasValue);
        var ownAspectRatio = IsOwn(bitFlag, nameof(AspectRatio)) && bitFlag.AspectRatio.HasValue();

        // A flag that sized itself keeps that size: a cascaded length would win over its Size, and a second length
        // would give its square proportions it never asked for. A flag that gave itself a ratio takes one cascaded
        // length at most, since two of them leave the browser nothing to work the ratio out for.
        var height = ownSize ? null : Height;
        var width = ownSize || (ownAspectRatio && height.HasValue()) ? null : Width;

        // Two lengths of the flag's own are its proportions already.
        if (AspectRatio.HasValue())
        {
            bitFlag.TakeFromCascade(nameof(AspectRatio), ownWidth && ownHeight ? null : AspectRatio, static f => f.AspectRatio, static (f, v) => f.AspectRatio = v);
        }

        if (AutoAlt.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(AutoAlt), AutoAlt.Value, static f => f.AutoAlt, static (f, v) => f.AutoAlt = v);
        }

        if (AutoTitle.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(AutoTitle), AutoTitle.Value, static f => f.AutoTitle, static (f, v) => f.AutoTitle = v);
        }

        if (Bordered.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Bordered), Bordered.Value, static f => f.Bordered, static (f, v) => f.Bordered = v);
        }

        if (Circular.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Circular), Circular.Value && ownRounded is false, static f => f.Circular, static (f, v) => f.Circular = v);
        }

        if (Classes is not null)
        {
            bitFlag.TakeFromCascade(nameof(Classes), Classes, static f => f.Classes, static (f, v) => f.Classes = v);
        }

        if (Emoji.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Emoji), Emoji.Value && ownImage is false, static f => f.Emoji, static (f, v) => f.Emoji = v);
        }

        if (FallbackTemplate is not null)
        {
            bitFlag.TakeFromCascade(nameof(FallbackTemplate), FallbackTemplate, static f => f.FallbackTemplate, static (f, v) => f.FallbackTemplate = v);
        }

        if (Fit.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Fit), Fit.Value, static f => f.Fit, static (f, v) => f.Fit = v);
        }

        if (Grayscale.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Grayscale), Grayscale.Value, static f => f.Grayscale, static (f, v) => f.Grayscale = v);
        }

        if (Height.HasValue())
        {
            bitFlag.TakeFromCascade(nameof(Height), height, static f => f.Height, static (f, v) => f.Height = v);
        }

        // The ImageAttributes are not written onto the flag: it renders the cascaded ones under its own, which is
        // what keeps both dictionaries uncopied and untouched, and a cascaded attribute taken away or changed since
        // gone or changed on the very next render.

        if (ImageSet.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(ImageSet), ImageSet.Value, static f => f.ImageSet, static (f, v) => f.ImageSet = v);
        }

        if (ImageSize.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(ImageSize), ImageSize.Value, static f => f.ImageSize, static (f, v) => f.ImageSize = v);
        }

        if (Loading.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Loading), Loading.Value, static f => f.Loading, static (f, v) => f.Loading = v);
        }

        if (Rounded.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Rounded), Rounded.Value, static f => f.Rounded, static (f, v) => f.Rounded = v);
        }

        if (Shadow.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Shadow), Shadow.Value, static f => f.Shadow, static (f, v) => f.Shadow = v);
        }

        if (Size.HasValue)
        {
            bitFlag.TakeFromCascade(nameof(Size), Size.Value, static f => f.Size, static (f, v) => f.Size = v);
        }

        if (SrcPattern.HasValue())
        {
            bitFlag.TakeFromCascade(nameof(SrcPattern), ownImageSet ? null : SrcPattern, static f => f.SrcPattern, static (f, v) => f.SrcPattern = v);
        }

        if (Styles is not null)
        {
            bitFlag.TakeFromCascade(nameof(Styles), Styles, static f => f.Styles, static (f, v) => f.Styles = v);
        }

        if (Width.HasValue())
        {
            bitFlag.TakeFromCascade(nameof(Width), width, static f => f.Width, static (f, v) => f.Width = v);
        }

        static bool IsOwn(BitFlag flag, string name) => flag.HasNotBeenSet(name) is false;
    }
}
