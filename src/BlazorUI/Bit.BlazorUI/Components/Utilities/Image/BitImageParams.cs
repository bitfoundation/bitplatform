namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitImage"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every image of a page or of an app agrees on - the loading and decoding
/// hints, the shape, the fade, the fit, and the fallback shown for a picture that cannot be had - rather
/// than what makes one image the image it is. The source, the alternate text and the templates are
/// deliberately not here: they are the content of a single image, and cascading them would give every
/// image on the page the same one.
/// </remarks>
public class BitImageParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitImage"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitImage value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitImage)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets the aspect ratio of the frame of the image.
    /// </summary>
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a border is rendered around the frame of the image.
    /// </summary>
    public bool? Bordered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame of the image is rendered as a circle.
    /// </summary>
    public bool? Circular { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS classes for the different parts of the image.
    /// </summary>
    public BitImageClassStyles? Classes { get; set; }

    /// <summary>
    /// Gets or sets how the shape of the image compares to the shape of its frame.
    /// </summary>
    public BitImageCover? Cover { get; set; }

    /// <summary>
    /// Gets or sets the CORS setting the image is requested with.
    /// </summary>
    public BitImageCrossOrigin? CrossOrigin { get; set; }

    /// <summary>
    /// Gets or sets the hint at whether the image may be decoded asynchronously.
    /// </summary>
    public BitImageDecoding? Decoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the image can be dragged by the user.
    /// </summary>
    public bool? Draggable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the image fades in when loaded.
    /// </summary>
    public bool? FadeIn { get; set; }

    /// <summary>
    /// Gets or sets the source of the image shown when the image's own source fails to load or is missing.
    /// </summary>
    public string? FallbackSrc { get; set; }

    /// <summary>
    /// Gets or sets the hint at the priority the image is fetched with.
    /// </summary>
    public BitImageFetchPriority? FetchPriority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame of the image is kept from growing wider than its container.
    /// </summary>
    public bool? Fluid { get; set; }


    /// <summary>
    /// Gets or sets the height of the frame of the image.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Gets or sets the additional attributes rendered on the img element.
    /// </summary>
    public Dictionary<string, object>? ImageAttributes { get; set; }

    /// <summary>
    /// Gets or sets how the image is scaled and cropped to fit its frame.
    /// </summary>
    public BitImageFit? ImageFit { get; set; }

    /// <summary>
    /// Gets or sets the position of the image inside its frame.
    /// </summary>
    public string? ImagePosition { get; set; }

    /// <summary>
    /// Gets or sets the browser-level loading behavior (lazy or eager) of the image.
    /// </summary>
    public BitImageLoading? Loading { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frame of the image expands to fill its parent container.
    /// </summary>
    public bool? MaximizeFrame { get; set; }

    /// <summary>
    /// Gets or sets how much of the address of the current page is sent to whoever serves the image.
    /// </summary>
    public BitImageReferrerPolicy? ReferrerPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the corners of the frame of the image are rounded.
    /// </summary>
    public bool? Rounded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a shadow is rendered under the frame of the image.
    /// </summary>
    public bool? Shadow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the image starts as visible and is hidden on error.
    /// </summary>
    public bool? StartVisible { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS styles for the different parts of the image.
    /// </summary>
    public BitImageClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the width of the frame of the image.
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitImage"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitImage"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitImage"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitImage"/>.
    /// </remarks>
    /// <param name="bitImage">
    /// The <see cref="BitImage"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitImage bitImage)
    {
        if (bitImage is null) return;

        UpdateBaseParameters(bitImage);

        if (AspectRatio.HasValue())
        {
            bitImage.TakeFromCascade(nameof(AspectRatio), AspectRatio, static i => i.AspectRatio, static (i, v) => i.AspectRatio = v);
        }

        if (Bordered.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Bordered), Bordered.Value, static i => i.Bordered, static (i, v) => i.Bordered = v);
        }

        if (Circular.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Circular), Circular.Value, static i => i.Circular, static (i, v) => i.Circular = v);
        }

        if (Classes is not null)
        {
            bitImage.TakeFromCascade(nameof(Classes), Classes, static i => i.Classes, static (i, v) => i.Classes = v);
        }

        if (Cover.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Cover), Cover.Value, static i => i.Cover, static (i, v) => i.Cover = v);
        }

        if (CrossOrigin.HasValue)
        {
            bitImage.TakeFromCascade(nameof(CrossOrigin), CrossOrigin.Value, static i => i.CrossOrigin, static (i, v) => i.CrossOrigin = v);
        }

        if (Decoding.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Decoding), Decoding.Value, static i => i.Decoding, static (i, v) => i.Decoding = v);
        }

        if (Draggable.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Draggable), Draggable.Value, static i => i.Draggable, static (i, v) => i.Draggable = v);
        }

        if (FadeIn.HasValue)
        {
            bitImage.TakeFromCascade(nameof(FadeIn), FadeIn.Value, static i => i.FadeIn, static (i, v) => i.FadeIn = v);
        }

        if (FallbackSrc.HasValue())
        {
            bitImage.TakeFromCascade(nameof(FallbackSrc), FallbackSrc, static i => i.FallbackSrc, static (i, v) => i.FallbackSrc = v);
        }

        if (FetchPriority.HasValue)
        {
            bitImage.TakeFromCascade(nameof(FetchPriority), FetchPriority.Value, static i => i.FetchPriority, static (i, v) => i.FetchPriority = v);
        }

        if (Fluid.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Fluid), Fluid.Value, static i => i.Fluid, static (i, v) => i.Fluid = v);
        }

        if (Height.HasValue())
        {
            bitImage.TakeFromCascade(nameof(Height), Height, static i => i.Height, static (i, v) => i.Height = v);
        }

        // The cascaded attributes are merged into a copy rather than into the image's own dictionary: that one
        // may well be an instance the page shares between several images, or keeps for itself, and writing
        // into it would hand the cascaded attributes to every one of them - or to the page - for good.
        if (ImageAttributes is not null && ImageAttributes.Count > 0)
        {
            Dictionary<string, object>? merged = null;

            foreach (var attribute in ImageAttributes)
            {
                if (bitImage.ImageAttributes.ContainsKey(attribute.Key)) continue;

                merged ??= new(bitImage.ImageAttributes, bitImage.ImageAttributes.Comparer);
                merged[attribute.Key] = attribute.Value;
            }

            if (merged is not null)
            {
                bitImage.ImageAttributes = merged;
            }
        }

        if (ImageFit.HasValue)
        {
            bitImage.TakeFromCascade(nameof(ImageFit), ImageFit.Value, static i => i.ImageFit, static (i, v) => i.ImageFit = v);
        }

        if (ImagePosition.HasValue())
        {
            bitImage.TakeFromCascade(nameof(ImagePosition), ImagePosition, static i => i.ImagePosition, static (i, v) => i.ImagePosition = v);
        }

        if (Loading.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Loading), Loading.Value, static i => i.Loading, static (i, v) => i.Loading = v);
        }

        if (MaximizeFrame.HasValue)
        {
            bitImage.TakeFromCascade(nameof(MaximizeFrame), MaximizeFrame.Value, static i => i.MaximizeFrame, static (i, v) => i.MaximizeFrame = v);
        }

        if (ReferrerPolicy.HasValue)
        {
            bitImage.TakeFromCascade(nameof(ReferrerPolicy), ReferrerPolicy.Value, static i => i.ReferrerPolicy, static (i, v) => i.ReferrerPolicy = v);
        }

        if (Rounded.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Rounded), Rounded.Value, static i => i.Rounded, static (i, v) => i.Rounded = v);
        }

        if (Shadow.HasValue)
        {
            bitImage.TakeFromCascade(nameof(Shadow), Shadow.Value, static i => i.Shadow, static (i, v) => i.Shadow = v);
        }

        if (StartVisible.HasValue)
        {
            bitImage.TakeFromCascade(nameof(StartVisible), StartVisible.Value, static i => i.StartVisible, static (i, v) => i.StartVisible = v);
        }

        if (Styles is not null)
        {
            bitImage.TakeFromCascade(nameof(Styles), Styles, static i => i.Styles, static (i, v) => i.Styles = v);
        }

        if (Width.HasValue())
        {
            bitImage.TakeFromCascade(nameof(Width), Width, static i => i.Width, static (i, v) => i.Width = v);
        }
    }
}
