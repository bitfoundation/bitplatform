namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitImage"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every image of a page or of an app agrees on - the loading and decoding
/// hints, the shape, the fade, the fit - rather than what makes one image the image it is. The source,
/// the alternate text and the templates are deliberately not here: they are the content of a single
/// image, and cascading them would give every image on the page the same one.
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
    /// Gets or sets the cover style to be used for the image.
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
    /// Gets or sets the hint at the priority the image is fetched with.
    /// </summary>
    public BitImageFetchPriority? FetchPriority { get; set; }

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

        if (AspectRatio.HasValue() && bitImage.HasNotBeenSet(nameof(AspectRatio)))
        {
            bitImage.AspectRatio = AspectRatio;

            bitImage.StyleBuilder.Reset();
        }

        if (Bordered.HasValue && bitImage.HasNotBeenSet(nameof(Bordered)))
        {
            bitImage.Bordered = Bordered.Value;

            bitImage.ClassBuilder.Reset();
        }

        if (Circular.HasValue && bitImage.HasNotBeenSet(nameof(Circular)))
        {
            bitImage.Circular = Circular.Value;

            bitImage.ClassBuilder.Reset();
        }

        if (Classes is not null && bitImage.HasNotBeenSet(nameof(Classes)))
        {
            bitImage.Classes = Classes;

            bitImage.ClassBuilder.Reset();
        }

        if (Cover.HasValue && bitImage.HasNotBeenSet(nameof(Cover)))
        {
            bitImage.Cover = Cover.Value;
        }

        if (CrossOrigin.HasValue && bitImage.HasNotBeenSet(nameof(CrossOrigin)))
        {
            bitImage.CrossOrigin = CrossOrigin.Value;
        }

        if (Decoding.HasValue && bitImage.HasNotBeenSet(nameof(Decoding)))
        {
            bitImage.Decoding = Decoding.Value;
        }

        if (Draggable.HasValue && bitImage.HasNotBeenSet(nameof(Draggable)))
        {
            bitImage.Draggable = Draggable.Value;
        }

        if (FadeIn.HasValue && bitImage.HasNotBeenSet(nameof(FadeIn)))
        {
            bitImage.FadeIn = FadeIn.Value;

            bitImage.ClassBuilder.Reset();
        }

        if (FetchPriority.HasValue && bitImage.HasNotBeenSet(nameof(FetchPriority)))
        {
            bitImage.FetchPriority = FetchPriority.Value;
        }

        if (Height.HasValue() && bitImage.HasNotBeenSet(nameof(Height)))
        {
            bitImage.Height = Height;

            bitImage.StyleBuilder.Reset();
        }

        if (ImageAttributes is not null)
        {
            foreach (var attribute in ImageAttributes)
            {
                if (bitImage.ImageAttributes.ContainsKey(attribute.Key)) continue;

                bitImage.ImageAttributes[attribute.Key] = attribute.Value;
            }
        }

        if (ImageFit.HasValue && bitImage.HasNotBeenSet(nameof(ImageFit)))
        {
            bitImage.ImageFit = ImageFit.Value;
        }

        if (ImagePosition.HasValue() && bitImage.HasNotBeenSet(nameof(ImagePosition)))
        {
            bitImage.ImagePosition = ImagePosition;
        }

        if (Loading.HasValue && bitImage.HasNotBeenSet(nameof(Loading)))
        {
            bitImage.Loading = Loading.Value;
        }

        if (MaximizeFrame.HasValue && bitImage.HasNotBeenSet(nameof(MaximizeFrame)))
        {
            bitImage.MaximizeFrame = MaximizeFrame.Value;

            bitImage.ClassBuilder.Reset();
        }

        if (ReferrerPolicy.HasValue && bitImage.HasNotBeenSet(nameof(ReferrerPolicy)))
        {
            bitImage.ReferrerPolicy = ReferrerPolicy.Value;
        }

        if (Rounded.HasValue && bitImage.HasNotBeenSet(nameof(Rounded)))
        {
            bitImage.Rounded = Rounded.Value;

            bitImage.ClassBuilder.Reset();
        }

        if (Shadow.HasValue && bitImage.HasNotBeenSet(nameof(Shadow)))
        {
            bitImage.Shadow = Shadow.Value;

            bitImage.ClassBuilder.Reset();
        }

        if (StartVisible.HasValue && bitImage.HasNotBeenSet(nameof(StartVisible)))
        {
            bitImage.StartVisible = StartVisible.Value;
        }

        if (Styles is not null && bitImage.HasNotBeenSet(nameof(Styles)))
        {
            bitImage.Styles = Styles;

            bitImage.StyleBuilder.Reset();
        }

        if (Width.HasValue() && bitImage.HasNotBeenSet(nameof(Width)))
        {
            bitImage.Width = Width;

            bitImage.StyleBuilder.Reset();
        }
    }
}
