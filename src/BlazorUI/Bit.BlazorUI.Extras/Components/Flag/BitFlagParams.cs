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

        // This runs on every render of every flag under the BitParams - a country picker renders two hundred of them -
        // so a value that drives the class or the style of the root only resets the builders when it differs from the
        // one the flag already holds: an unchanged one would rebuild both strings on every render for nothing.
        if (AspectRatio.HasValue() && bitFlag.HasNotBeenSet(nameof(AspectRatio)))
        {
            // Two lengths of the flag's own are its proportions already.
            var aspectRatio = ownWidth && ownHeight ? null : AspectRatio;

            if (bitFlag.AspectRatio != aspectRatio)
            {
                bitFlag.AspectRatio = aspectRatio;

                bitFlag.ClassBuilder.Reset();
                bitFlag.StyleBuilder.Reset();
            }
        }

        if (AutoAlt.HasValue && bitFlag.HasNotBeenSet(nameof(AutoAlt)))
        {
            bitFlag.AutoAlt = AutoAlt.Value;
        }

        if (AutoTitle.HasValue && bitFlag.HasNotBeenSet(nameof(AutoTitle)))
        {
            bitFlag.AutoTitle = AutoTitle.Value;
        }

        if (Bordered.HasValue && bitFlag.HasNotBeenSet(nameof(Bordered)) && bitFlag.Bordered != Bordered.Value)
        {
            bitFlag.Bordered = Bordered.Value;

            bitFlag.ClassBuilder.Reset();
            bitFlag.StyleBuilder.Reset();
        }

        if (Circular.HasValue && bitFlag.HasNotBeenSet(nameof(Circular)))
        {
            var circular = Circular.Value && ownRounded is false;

            if (bitFlag.Circular != circular)
            {
                bitFlag.Circular = circular;

                bitFlag.ClassBuilder.Reset();
                bitFlag.StyleBuilder.Reset();
            }
        }

        if (Classes is not null && bitFlag.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitFlag.Classes, Classes) is false)
        {
            bitFlag.Classes = Classes;

            bitFlag.ClassBuilder.Reset();
        }

        if (Emoji.HasValue && bitFlag.HasNotBeenSet(nameof(Emoji)))
        {
            var emoji = Emoji.Value && ownImage is false;

            if (bitFlag.Emoji != emoji)
            {
                bitFlag.Emoji = emoji;

                bitFlag.ClassBuilder.Reset();
                bitFlag.StyleBuilder.Reset();
            }
        }

        if (FallbackTemplate is not null && bitFlag.HasNotBeenSet(nameof(FallbackTemplate)))
        {
            bitFlag.FallbackTemplate = FallbackTemplate;
        }

        if (Fit.HasValue && bitFlag.HasNotBeenSet(nameof(Fit)) && bitFlag.Fit != Fit.Value)
        {
            bitFlag.Fit = Fit.Value;

            // Whether the frame is cut to the flag, which is written into its style, follows the fit too.
            bitFlag.ClassBuilder.Reset();
            bitFlag.StyleBuilder.Reset();
        }

        if (Grayscale.HasValue && bitFlag.HasNotBeenSet(nameof(Grayscale)) && bitFlag.Grayscale != Grayscale.Value)
        {
            bitFlag.Grayscale = Grayscale.Value;

            bitFlag.ClassBuilder.Reset();
        }

        if (Height.HasValue() && bitFlag.HasNotBeenSet(nameof(Height)) && bitFlag.Height != height)
        {
            bitFlag.Height = height;

            bitFlag.ClassBuilder.Reset();
            bitFlag.StyleBuilder.Reset();
        }

        // The ImageAttributes are not written onto the flag: it renders the cascaded ones under its own, which is
        // what keeps both dictionaries uncopied and untouched, and a cascaded attribute taken away or changed since
        // gone or changed on the very next render.

        if (ImageSet.HasValue && bitFlag.HasNotBeenSet(nameof(ImageSet)))
        {
            bitFlag.ImageSet = ImageSet.Value;
        }

        if (ImageSize.HasValue && bitFlag.HasNotBeenSet(nameof(ImageSize)))
        {
            bitFlag.ImageSize = ImageSize.Value;
        }

        if (Loading.HasValue && bitFlag.HasNotBeenSet(nameof(Loading)))
        {
            bitFlag.Loading = Loading.Value;
        }

        if (Rounded.HasValue && bitFlag.HasNotBeenSet(nameof(Rounded)) && bitFlag.Rounded != Rounded.Value)
        {
            bitFlag.Rounded = Rounded.Value;

            bitFlag.ClassBuilder.Reset();
            bitFlag.StyleBuilder.Reset();
        }

        if (Shadow.HasValue && bitFlag.HasNotBeenSet(nameof(Shadow)) && bitFlag.Shadow != Shadow.Value)
        {
            bitFlag.Shadow = Shadow.Value;

            bitFlag.ClassBuilder.Reset();
            bitFlag.StyleBuilder.Reset();
        }

        if (Size.HasValue && bitFlag.HasNotBeenSet(nameof(Size)) && bitFlag.Size != Size.Value)
        {
            bitFlag.Size = Size.Value;

            bitFlag.ClassBuilder.Reset();
        }

        if (SrcPattern.HasValue() && bitFlag.HasNotBeenSet(nameof(SrcPattern)))
        {
            bitFlag.SrcPattern = ownImageSet ? null : SrcPattern;
        }

        if (Styles is not null && bitFlag.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitFlag.Styles, Styles) is false)
        {
            bitFlag.Styles = Styles;

            bitFlag.StyleBuilder.Reset();
        }

        if (Width.HasValue() && bitFlag.HasNotBeenSet(nameof(Width)) && bitFlag.Width != width)
        {
            bitFlag.Width = width;

            bitFlag.ClassBuilder.Reset();
            bitFlag.StyleBuilder.Reset();
        }

        static bool IsOwn(BitFlag flag, string name) => flag.HasNotBeenSet(name) is false;
    }
}
