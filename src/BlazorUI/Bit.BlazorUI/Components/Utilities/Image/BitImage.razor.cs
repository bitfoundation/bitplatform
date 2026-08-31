using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// An image is a graphic representation of something (e.g photo or illustration).
/// </summary>
/// <remarks>
/// The component is an img inside a frame, and nearly everything it offers is about the relationship
/// between the two. The frame takes the size - <see cref="Width"/>, <see cref="Height"/>,
/// <see cref="AspectRatio"/>, <see cref="MaximizeFrame"/> - and the shape - <see cref="Rounded"/>,
/// <see cref="Circular"/>, <see cref="Bordered"/> - and clips whatever falls outside it, while
/// <see cref="ImageFit"/> and <see cref="ImagePosition"/> decide what the image does inside it once
/// the two turn out to be different shapes.
/// <br />
/// The second half is what happens before the image is there. The component follows the browser's own
/// load and error events through <see cref="BitImageState"/>, and the state decides what is on screen:
/// the image is hidden until it has loaded (unless <see cref="StartVisible"/> says otherwise) and
/// fades in with <see cref="FadeIn"/>, a <see cref="PlaceholderSrc"/> holds the frame meanwhile, a
/// <see cref="LoadingTemplate"/> and an <see cref="ErrorTemplate"/> stand in its place, and a
/// <see cref="FallbackSrc"/> is tried once before the error state is reached at all.
/// <br />
/// What the browser itself decides is reachable rather than reimplemented: <see cref="Loading"/>,
/// <see cref="Decoding"/>, <see cref="FetchPriority"/>, <see cref="CrossOrigin"/> and
/// <see cref="ReferrerPolicy"/> are the img attributes of the same names, and <see cref="Srcset"/>
/// with <see cref="Sizes"/> is the responsive-image mechanism, so the browser picks the file rather
/// than the page guessing at the device - with <see cref="Sources"/> for the two things a srcset
/// cannot express, a different crop at a different viewport and a modern format offered to whoever can
/// read it. Anything else an img accepts goes through <see cref="ImageAttributes"/>, which is merged
/// with - rather than overwritten by - what the parameters set.
/// <br />
/// What is left is the frame as a surface rather than as a box: <see cref="Shadow"/> lifts it off the
/// page, <see cref="ChildContent"/> lays a caption or a scrim over it, and <see cref="OnClick"/> turns
/// it into a button that answers the keyboard as well as the pointer.
/// </remarks>
public partial class BitImage : BitComponentBase
{
    private BitImageState _loadingState;

    /// <summary>
    /// The source actually rendered, which is <see cref="Src"/> until an error swaps in the
    /// <see cref="FallbackSrc"/>.
    /// </summary>
    private string? _src;

    /// <summary>
    /// Whether the fallback has already been tried, which is what keeps a failing fallback from being
    /// requested again and again by its own error.
    /// </summary>
    private bool _fallbackApplied;

    /// <summary>
    /// Whether a parameter change has moved the state back to loading without anyone being told yet;
    /// see <see cref="OnParametersSetAsync"/>.
    /// </summary>
    private bool _stateChangePending;

    /// <summary>
    /// The key of the img element. Changing it replaces the element rather than patching it, which is
    /// the only way to make the browser fetch a source it already has an answer for - see
    /// <see cref="ReloadAsync"/>.
    /// </summary>
    private int _reloadKey;

    private ElementReference _imageElement;

    private bool _isClickable => IsEnabled && OnClick.HasDelegate;

    // The placeholder stands in for an image that is not on screen, which is as true of one that has
    // failed as of one still on its way: it is taken away by the image arriving rather than by time.
    private string? _placeholderSrc => _loadingState is not BitImageState.Loaded && PlaceholderSrc.HasValue()
                                        ? PlaceholderSrc
                                        : null;

    private bool _hasSources
    {
        get
        {
            if (Sources is null) return false;

            foreach (var source in Sources)
            {
                if (source is not null && source.Srcset.HasValue()) return true;
            }

            return false;
        }
    }



    /// <summary>
    /// Gets or sets the cascading parameters for the image component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple image components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitImageParams.ParamName)]
    public BitImageParams? CascadingParameters { get; set; }



    /// <summary>
    /// Specifies an alternate text for the image.
    /// </summary>
    /// <remarks>
    /// The attribute is always rendered, so an image that is given none is announced as decorative
    /// (alt="") rather than as an image with no name - which is what a screen reader falls back to
    /// reading the file name of. An image that carries meaning needs a text that carries the same
    /// meaning; one that is purely decorative is better left without.
    /// <br />
    /// Where the image is made clickable through <see cref="OnClick"/>, this is also the accessible
    /// name of the resulting button, so it says what the click does rather than what the image shows.
    /// </remarks>
    [Parameter] public string? Alt { get; set; }

    /// <summary>
    /// The aspect ratio of the frame of the image, as a CSS aspect-ratio value (e.g. "16/9" or "1").
    /// </summary>
    /// <remarks>
    /// This is what reserves the room the image will need before it has arrived, so the content below
    /// it is not pushed down the moment it does - the layout shift that a page is measured by. A frame
    /// given a width and an aspect ratio needs no height, and one given neither a width nor a height
    /// takes the width of its parent.
    /// <br />
    /// It pairs with <see cref="ImageFit"/>: the ratio decides the shape of the frame, and the fit
    /// decides what the image does inside it where its own shape is a different one.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Renders a border around the frame of the image.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Bordered { get; set; }

    /// <summary>
    /// The content rendered over the image, filling the frame.
    /// </summary>
    /// <remarks>
    /// This is the layer a caption, a badge, a play button or a gradient scrim goes on. It is laid over
    /// the whole frame, so it needs a frame with a size to be laid over - a <see cref="Width"/> and a
    /// <see cref="Height"/>, an <see cref="AspectRatio"/>, or a <see cref="MaximizeFrame"/>.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Renders the frame of the image as a circle.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The frame is what is rounded, and it is only a circle while it is a square, so this is normally
    /// paired with an equal <see cref="Width"/> and <see cref="Height"/> - or an
    /// <see cref="AspectRatio"/> of 1 - and with a <see cref="BitImageFit.Cover"/> fit, which is what
    /// keeps an image of another shape from being cropped off center.
    /// <br />
    /// Takes precedence over <see cref="Rounded"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Circular { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitImage.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitImageClassStyles? Classes { get; set; }

    /// <summary>
    /// Specifies the cover style to be used for this image.
    /// </summary>
    /// <remarks>
    /// Only <see cref="BitImageFit.CenterCover"/> and <see cref="BitImageFit.CenterContain"/> read it;
    /// see <see cref="BitImageCover"/>. The default is <see cref="BitImageCover.Portrait"/>.
    /// </remarks>
    [Parameter] public BitImageCover? Cover { get; set; }

    /// <summary>
    /// Specifies the CORS setting the image is requested with.
    /// </summary>
    /// <remarks>
    /// Only needed where the pixels of a cross-origin image have to be readable - drawn into a canvas,
    /// uploaded as a WebGL texture. Setting it makes the request fail outright where the other origin
    /// answers without the matching CORS headers, so it is set where it is needed rather than by default.
    /// </remarks>
    [Parameter] public BitImageCrossOrigin? CrossOrigin { get; set; }

    /// <summary>
    /// Hints the browser at whether the image may be decoded asynchronously.
    /// </summary>
    [Parameter] public BitImageDecoding? Decoding { get; set; }

    /// <summary>
    /// Specifies whether the image can be dragged by the user.
    /// </summary>
    /// <remarks>
    /// Browsers make an image draggable of itself, which is what lets it be dropped into another
    /// application - and also what makes a press on an image inside a carousel or a swipeable surface
    /// start a drag instead of the gesture that was meant. Setting this to false is what turns that off.
    /// </remarks>
    [Parameter] public bool? Draggable { get; set; }

    /// <summary>
    /// The custom template used to show the error state of the image.
    /// </summary>
    /// <remarks>
    /// Rendered once the image has failed - and, where a <see cref="FallbackSrc"/> is provided, once
    /// that one has failed as well. The image itself is hidden in that state, so this is all there is
    /// to see; a <see cref="FallbackSrc"/> is the other way to answer the same case.
    /// </remarks>
    [Parameter] public RenderFragment? ErrorTemplate { get; set; }

    /// <summary>
    /// If true, fades the image in when loaded.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The fade runs at the moment the image becomes visible rather than when the component is
    /// rendered, so it is the arrival of the image that is animated. It collapses to nothing under
    /// prefers-reduced-motion unless
    /// <see cref="BitComponentBase.ForceAnimation"/> says otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FadeIn { get; set; }

    /// <summary>
    /// The source of the image to show when the one given by <see cref="Src"/> fails to load.
    /// </summary>
    /// <remarks>
    /// It is tried exactly once, so a fallback that fails as well leaves the component in the
    /// <see cref="BitImageState.Error"/> state rather than asking for it again. It is also what is
    /// shown when no <see cref="Src"/> is given at all, which is what makes a missing avatar and a
    /// broken one look the same.
    /// <br />
    /// A fallback belongs to the page rather than to the network: something already cached or inlined
    /// as a data URI, since whatever kept the first image from arriving may well keep this one away too.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetSrc))]
    public string? FallbackSrc { get; set; }

    /// <summary>
    /// Hints the browser at the priority this image is fetched with, relative to the other resources of the page.
    /// </summary>
    [Parameter] public BitImageFetchPriority? FetchPriority { get; set; }

    /// <summary>
    /// The image height value.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// ("5rem", "50%", "calc(100vh - 2rem)") is accepted. It sizes the FRAME rather than the image:
    /// what the image does inside a frame of that height is <see cref="ImageFit"/>'s answer.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the image's parameters.
    /// </summary>
    /// <remarks>
    /// The dictionary is merged with the attributes the component builds itself rather than replaced
    /// by them: an attribute that has a parameter of its own here (src, alt, loading, ...) is written
    /// over only while that parameter is set, so anything the parameters do not cover - usemap, ismap,
    /// elementtiming, a data-* attribute - reaches the img untouched.
    /// </remarks>
    [Parameter] public Dictionary<string, object> ImageAttributes { get; set; } = [];

    /// <summary>
    /// Used to determine how the image is scaled and cropped to fit the frame.
    /// </summary>
    [Parameter] public BitImageFit? ImageFit { get; set; }

    /// <summary>
    /// The position of the image inside its frame, as a CSS object-position value (e.g. "top", "50% 25%").
    /// </summary>
    /// <remarks>
    /// It decides which part of the image survives the crop, and so it only has an effect on a fit that
    /// crops: a <see cref="BitImageFit.Cover"/> portrait defaults to being cropped around its middle,
    /// which is the wrong half of a photograph of a face.
    /// </remarks>
    [Parameter] public string? ImagePosition { get; set; }

    /// <summary>
    /// Allows for browser-level image loading (lazy or eager).
    /// </summary>
    /// <remarks>
    /// A lazy image is only fetched once the browser expects it to be needed, which it decides by where
    /// the image sits in the page - so a lazy image keeps the room it will take up while it waits,
    /// rather than being taken out of the layout the way a loading image otherwise is. Give it a
    /// <see cref="Height"/> or an <see cref="AspectRatio"/> so that room is the right size.
    /// <br />
    /// Never lazy for what is already on the first screen: the browser would then be told to wait for
    /// the one image the reader is looking at.
    /// </remarks>
    [Parameter] public BitImageLoading? Loading { get; set; }

    /// <summary>
    /// The custom template used to show the loading state of the image.
    /// </summary>
    /// <remarks>
    /// Rendered while the image is on its way, and only while <see cref="StartVisible"/> is false -
    /// with the image visible from the start there is nothing for it to stand in for.
    /// </remarks>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// If true, the image frame will expand to fill its parent container.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The frame takes the full width and height of its parent, and the image covers it. An explicit
    /// <see cref="ImageFit"/> still has the last word over that cover.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool MaximizeFrame { get; set; }

    /// <summary>
    /// Callback for when the image clicked.
    /// </summary>
    /// <remarks>
    /// Assigning it makes the image a button: it becomes focusable, is announced as a button with the
    /// <see cref="Alt"/> as its name, and answers the Enter and the Space keys as well as the pointer.
    /// A disabled image answers neither.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the image fails to load.
    /// </summary>
    /// <remarks>
    /// Reports every failed attempt, including the one that is answered by falling back to the
    /// <see cref="FallbackSrc"/> - which is not yet the <see cref="BitImageState.Error"/> state.
    /// </remarks>
    [Parameter] public EventCallback OnError { get; set; }

    /// <summary>
    /// Callback for when the image has been loaded successfully.
    /// </summary>
    [Parameter] public EventCallback OnLoad { get; set; }

    /// <summary>
    /// Optional callback method for when the image load state has changed.
    /// The 'loadState' parameter indicates the current state of the Image.
    /// </summary>
    /// <remarks>
    /// Called on every transition of <see cref="BitImageState"/>, which includes the return to
    /// <see cref="BitImageState.Loading"/> when the <see cref="Src"/> changes or
    /// <see cref="ReloadAsync"/> is called.
    /// </remarks>
    [Parameter] public EventCallback<BitImageState> OnLoadingStateChange { get; set; }

    /// <summary>
    /// The source of a placeholder image shown, blurred, while the image itself is still loading.
    /// </summary>
    /// <remarks>
    /// This is the blur-up placeholder: a tiny, heavily compressed copy of the same picture - normally
    /// inlined as a data URI so it costs no request - which fills the frame from the first frame and is
    /// replaced the moment the real one arrives. It fills the frame, so it needs a frame with a size:
    /// a <see cref="Width"/> and a <see cref="Height"/>, an <see cref="AspectRatio"/>, or a
    /// <see cref="MaximizeFrame"/>.
    /// </remarks>
    [Parameter] public string? PlaceholderSrc { get; set; }

    /// <summary>
    /// Specifies how much of the address of the current page is sent to whoever serves the image.
    /// </summary>
    [Parameter] public BitImageReferrerPolicy? ReferrerPolicy { get; set; }

    /// <summary>
    /// Rounds the corners of the frame of the image.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Rounded { get; set; }

    /// <summary>
    /// The value of the sizes attribute of the image, which tells the browser how wide the image will
    /// be laid out at before it knows the layout (e.g. "(max-width: 600px) 100vw, 50vw").
    /// </summary>
    /// <remarks>
    /// It is only read together with a width-descriptor <see cref="Srcset"/>, and it is what turns that
    /// list of widths into a choice: without it the browser assumes the image takes the full width of
    /// the viewport and fetches accordingly.
    /// </remarks>
    [Parameter] public string? Sizes { get; set; }

    /// <summary>
    /// Renders a shadow under the frame of the image, lifting it off the surface it sits on.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The shadow is the theme's card elevation, so it is the same lift every other raised surface of
    /// the library has rather than a value written here.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Shadow { get; set; }

    /// <summary>
    /// The alternative sources of the image, offered to the browser ahead of <see cref="Src"/>.
    /// </summary>
    /// <remarks>
    /// This is the art-direction and the format-negotiation half of responsive images, which
    /// <see cref="Srcset"/> cannot express: a different CROP of the subject at a different viewport, and
    /// a modern format (AVIF, WebP) offered to whoever can read it. The browser walks them in order and
    /// takes the first one it is satisfied by, so the order is the priority and <see cref="Src"/> stays
    /// the answer that every browser understands.
    /// <br />
    /// The image is wrapped in a picture element while there is at least one source with a srcset; the
    /// picture lays nothing out of its own, so everything about the frame and the fit is unchanged.
    /// <br />
    /// Changing them does not return the component to the loading state the way changing the
    /// <see cref="Src"/> does. A collection is compared by reference, and a page that writes the sources
    /// inline builds a new one on every render, so a reset here would hide a loaded image again every
    /// time anything around it changed.
    /// </remarks>
    [Parameter] public IEnumerable<BitImageSource>? Sources { get; set; }

    /// <summary>
    /// Specifies the src of image.
    /// </summary>
    /// <remarks>
    /// Changing it returns the component to the <see cref="BitImageState.Loading"/> state and drops any
    /// <see cref="FallbackSrc"/> that a previous failure had swapped in.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetSrc))]
    public string? Src { get; set; }

    /// <summary>
    /// The set of image sources the browser may choose from, with their width or density descriptors
    /// (e.g. "photo-480.jpg 480w, photo-960.jpg 960w").
    /// </summary>
    /// <remarks>
    /// The browser picks one of them by the size it is laid out at and by the pixel density of the
    /// screen, which is a decision the page cannot make for it. <see cref="Src"/> stays the fallback
    /// for a browser that reads none of this, and pairing a width-descriptor list with
    /// <see cref="Sizes"/> is what lets the choice be made before the layout is known.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetSrc))]
    public string? Srcset { get; set; }

    /// <summary>
    /// If true, the image starts as visible and is hidden on error. Otherwise, the image is hidden until it is successfully loaded.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Hiding the image until it has loaded is what keeps a half-drawn or broken image off the page,
    /// at the cost of the frame collapsing while it waits - which is what an <see cref="AspectRatio"/>,
    /// a <see cref="PlaceholderSrc"/> or a <see cref="LoadingTemplate"/> is for. Starting visible is the
    /// other trade: the browser's own progressive rendering, and no room reserved for what is not there.
    /// </remarks>
    [Parameter] public bool StartVisible { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitImage.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitImageClassStyles? Styles { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the image.
    /// </summary>
    /// <remarks>
    /// A tooltip is shown to a mouse and to nothing else - not to a touch screen, and not reliably to a
    /// screen reader - so it is an aside rather than a place for anything the image cannot do without.
    /// That belongs in <see cref="Alt"/>.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The image width value.
    /// </summary>
    /// <inheritdoc cref="Height" path="/remarks"/>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    /// <summary>
    /// Gets the reference to the img element of the component.
    /// </summary>
    /// <remarks>
    /// This is what a page reaches for to do something to the image itself rather than to the component
    /// around it - drawing it into a canvas, reading its natural size, handing it to an interop call.
    /// It is not captured before the first render.
    /// <br />
    /// <see cref="BitComponentBase.RootElement"/> is the frame around it, which is what carries the
    /// size and the shape.
    /// </remarks>
    public ElementReference ImageElement => _imageElement;

    /// <summary>
    /// The current loading state of the image.
    /// </summary>
    public BitImageState LoadingState => _loadingState;

    /// <summary>
    /// Gives the browser focus to the img element of the component.
    /// </summary>
    /// <remarks>
    /// Nothing but a clickable image (one with an <see cref="OnClick"/>) or one given an explicit
    /// <see cref="BitComponentBase.TabIndex"/> is focusable at all, so anywhere else the call does
    /// nothing. Nothing is captured before the first render either, where it does nothing rather than
    /// fail on an element that is not there.
    /// </remarks>
    public ValueTask FocusAsync()
    {
        return _imageElement.Context is null ? ValueTask.CompletedTask : _imageElement.FocusAsync();
    }

    /// <summary>
    /// Gives the browser focus to the img element of the component.
    /// </summary>
    /// <param name="preventScroll">A Boolean value indicating whether or not the browser should scroll
    /// the document to bring the newly-focused element into view. A value of false for preventScroll (the default)
    /// means that the browser will scroll the element into view after focusing it.
    /// If preventScroll is set to true, no scrolling will occur.</param>
    /// <inheritdoc cref="FocusAsync()" path="/remarks"/>
    public ValueTask FocusAsync(bool preventScroll)
    {
        return _imageElement.Context is null ? ValueTask.CompletedTask : _imageElement.FocusAsync(preventScroll);
    }

    /// <summary>
    /// Requests the image again from the beginning, whichever state it is in.
    /// </summary>
    /// <remarks>
    /// The component returns to the <see cref="BitImageState.Loading"/> state, forgets that a
    /// <see cref="FallbackSrc"/> has been tried, and replaces the img element rather than patching it -
    /// which is what makes the browser fetch a source it already holds an answer for. This is the
    /// answer to a failure that was the network's rather than the image's.
    /// </remarks>
    public Task ReloadAsync()
    {
        _reloadKey++;
        _fallbackApplied = false;
        _src = Src.HasValue() ? Src : FallbackSrc;

        return SetLoadingStateAsync(BitImageState.Loading, forceRender: true);
    }



    protected override string RootElementClass => "bit-img";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => MaximizeFrame ? "bit-img-max" : string.Empty);

        ClassBuilder.Register(() => Circular ? "bit-img-cir" : (Rounded ? "bit-img-rnd" : string.Empty));

        ClassBuilder.Register(() => Bordered ? "bit-img-brd" : string.Empty);

        ClassBuilder.Register(() => Shadow ? "bit-img-shd" : string.Empty);

        ClassBuilder.Register(() => _isClickable ? "bit-img-clk" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Width.HasValue() ? $"width:{GetValueWithUnit(Width)}" : string.Empty);

        StyleBuilder.Register(() => Height.HasValue() ? $"height:{GetValueWithUnit(Height)}" : string.Empty);

        StyleBuilder.Register(() => AspectRatio.HasValue() ? $"aspect-ratio:{AspectRatio}" : string.Empty);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitImageParams))]
    protected override async Task OnParametersSetAsync()
    {
        CascadingParameters?.UpdateParameters(this);

        // The source change that put the component back into the loading state is reported here rather
        // than where it is noticed: that happens while the parameters are still being assigned, so the
        // callback itself may well be one of the parameters that has not been assigned yet.
        if (_stateChangePending)
        {
            _stateChangePending = false;

            await OnLoadingStateChange.InvokeAsync(_loadingState);
        }

        await base.OnParametersSetAsync();
    }



    /// <summary>
    /// A new source is a new image, so whatever the previous one ended up as is no longer the answer:
    /// the state returns to loading and the fallback is available again.
    /// </summary>
    private void OnSetSrc()
    {
        _fallbackApplied = false;
        _src = Src.HasValue() ? Src : FallbackSrc;

        if (_loadingState == BitImageState.Loading) return;

        _loadingState = BitImageState.Loading;
        _stateChangePending = true;
    }

    /// <summary>
    /// A CSS length from a parameter that also accepts a bare number, which is read as a pixel count.
    /// The number is parsed with the invariant culture, since it is a value written into a stylesheet
    /// rather than one shown to a user: read with the current one, "9.5" would be a different length in
    /// a culture whose decimal separator is the comma, and none at all in the CSS that came out of it.
    /// </summary>
    private static string GetValueWithUnit(string? val)
    {
        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return FormattableString.Invariant($"{result}px");
        }

        return val!;
    }

    /// <summary>
    /// Everything the img element renders, in one dictionary.
    /// </summary>
    /// <remarks>
    /// The attributes are merged into a copy of <see cref="ImageAttributes"/> rather than written
    /// beside its splat, because the two are not the same thing: an attribute written after an
    /// "@attributes" directive with a null value REMOVES the splatted attribute of that name instead of
    /// leaving it alone, so every parameter left unset would take away the attribute of the same name a
    /// page had put in the dictionary. Merging writes only what is actually set.
    /// <br />
    /// The comparer is case insensitive because HTML attribute names are: an attribute put in the
    /// dictionary as "srcSet" and one written here as "srcset" are the same attribute, and the render
    /// tree would otherwise emit both.
    /// </remarks>
    private Dictionary<string, object> GetImageAttributes()
    {
        Dictionary<string, object> attributes = new(ImageAttributes.Count + 14, StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in ImageAttributes)
        {
            attributes[attribute.Key] = attribute.Value;
        }

        // An image is never left without an alt: one that has no text of its own is decorative, which
        // is what an empty alt says, and is skipped by a screen reader rather than read out as a URL.
        if (Alt is not null || attributes.ContainsKey("alt") is false)
        {
            attributes["alt"] = Alt ?? string.Empty;
        }

        Set(attributes, "src", _src);
        Set(attributes, "srcset", Srcset);
        Set(attributes, "sizes", Sizes);
        Set(attributes, "title", Title);
        Set(attributes, "loading", Loading?.ToString().ToLowerInvariant());
        Set(attributes, "decoding", Decoding?.ToString().ToLowerInvariant());
        Set(attributes, "fetchpriority", FetchPriority?.ToString().ToLowerInvariant());
        Set(attributes, "crossorigin", CrossOrigin switch
        {
            BitImageCrossOrigin.Anonymous => "anonymous",
            BitImageCrossOrigin.UseCredentials => "use-credentials",
            _ => null
        });
        Set(attributes, "referrerpolicy", ReferrerPolicy switch
        {
            BitImageReferrerPolicy.NoReferrer => "no-referrer",
            BitImageReferrerPolicy.NoReferrerWhenDowngrade => "no-referrer-when-downgrade",
            BitImageReferrerPolicy.Origin => "origin",
            BitImageReferrerPolicy.OriginWhenCrossOrigin => "origin-when-cross-origin",
            BitImageReferrerPolicy.SameOrigin => "same-origin",
            BitImageReferrerPolicy.StrictOrigin => "strict-origin",
            BitImageReferrerPolicy.StrictOriginWhenCrossOrigin => "strict-origin-when-cross-origin",
            BitImageReferrerPolicy.UnsafeUrl => "unsafe-url",
            _ => null
        });
        Set(attributes, "draggable", Draggable.HasValue ? (Draggable.Value ? "true" : "false") : null);

        // The label names the image rather than the frame: an aria-label on a div with no role of its
        // own is ignored, so on the frame it would say nothing at all. Where it is set it is the
        // accessible name, and the alt beside it is what is shown if the image never arrives.
        Set(attributes, "aria-label", AriaLabel);

        // A clickable image is a button rather than an image: it answers the pointer and the keyboard,
        // so it is announced and reached as one, with the alt as its name. A disabled one keeps the
        // role and says so, which is what has it announced as disabled rather than as missing.
        if (OnClick.HasDelegate)
        {
            attributes["role"] = "button";

            if (IsEnabled is false)
            {
                attributes["aria-disabled"] = "true";
            }
        }

        Set(attributes, "tabindex", TabIndex ?? (_isClickable ? "0" : null));

        Set(attributes, "style", GetImageStyles());

        // A class list a page put in ImageAttributes is joined to the component's rather than replaced
        // by it, the same way the two styles are: both are lists, and neither is the other's default.
        attributes["class"] = GetImageClasses(attributes.TryGetValue("class", out var splattedClass)
                                                ? splattedClass?.ToString()
                                                : null);

        return attributes;

        static void Set(Dictionary<string, object> attributes, string name, string? value)
        {
            if (value.HasNoValue()) return;

            attributes[name] = value!;
        }
    }

    private string? GetPlaceholderStyles()
    {
        // The placeholder is cropped by the same frame the image is, so it is positioned the same way -
        // a blur-up of a photograph cropped to its top edge that centered itself would jump on arrival.
        var style = ImagePosition.HasValue() ? $"object-position:{ImagePosition}" : null;

        return JoinStyles(style, Styles?.Placeholder);
    }

    private string? GetImageStyles()
    {
        var style = ImagePosition.HasValue() ? $"object-position:{ImagePosition}" : null;

        // The style a page put in ImageAttributes comes first, so what the component sets wins where
        // the two name the same declaration.
        if (ImageAttributes.Count > 0)
        {
            foreach (var attribute in ImageAttributes)
            {
                if (string.Equals(attribute.Key, "style", StringComparison.OrdinalIgnoreCase) is false) continue;

                style = JoinStyles(attribute.Value?.ToString(), style);
                break;
            }
        }

        return JoinStyles(style, Styles?.Image);
    }

    private string GetImageClasses(string? splattedClass = null)
    {
        StringBuilder className = new StringBuilder();

        className.Append("bit-img-img");

        className.Append(ImageFit switch
        {
            BitImageFit.None => " bit-img-non",
            BitImageFit.Center => " bit-img-ctr",
            BitImageFit.Contain => " bit-img-cnt",
            BitImageFit.Cover => " bit-img-cvr",
            BitImageFit.CenterCover => " bit-img-ccv",
            BitImageFit.CenterContain => " bit-img-cct",
            BitImageFit.Fill => " bit-img-fil",
            BitImageFit.ScaleDown => " bit-img-scd",
            _ => null
        });

        if (ImageFit.HasValue is false && (Width.HasValue() ^ Height.HasValue()))
        {
            if (Width.HasValue())
            {
                className.Append(" bit-img-ihw");
            }
            else
            {
                className.Append(" bit-img-ihh");
            }
        }

        className.Append(Cover is BitImageCover.Landscape ? " bit-img-lan" : " bit-img-por");

        if (_loadingState is BitImageState.Loaded || (_loadingState is BitImageState.Loading && StartVisible))
        {
            className.Append(" bit-img-vis");

            // The fade belongs to the moment the image becomes visible rather than to the mounting of
            // the component: the class arrives with the one above it, which is the render the load
            // event caused, so the animation starts where the image does.
            if (FadeIn)
            {
                className.Append(" bit-img-fde");
            }
        }
        else
        {
            className.Append(" bit-img-hid");

            // A lazy image is only fetched once the browser expects it to be needed, and it decides
            // that from where the image sits in the page - which a display:none element does not. So a
            // hidden lazy image keeps its box and is merely not painted, or it would never load at all
            // and so never stop being hidden.
            if (Loading is BitImageLoading.Lazy)
            {
                className.Append(" bit-img-lzy");
            }
        }

        if (Classes?.Image.HasValue() ?? false)
        {
            className.Append(' ').Append(Classes?.Image);
        }

        if (splattedClass.HasValue())
        {
            className.Append(' ').Append(splattedClass);
        }

        return className.ToString();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    // The Enter key of a button activates it on the way down, and the Space key on the way up. Nothing
    // is rendered as focusable at all unless there is a click handler to answer, so a page that never
    // makes the image clickable pays for neither of these.
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (_isClickable is false) return;

        if (e.Key != "Enter") return;

        await OnClick.InvokeAsync(new MouseEventArgs { Type = "click", Detail = 0 });
    }

    private async Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (_isClickable is false) return;

        if (e.Key is not (" " or "Spacebar")) return;

        await OnClick.InvokeAsync(new MouseEventArgs { Type = "click", Detail = 0 });
    }

    private async Task HandleOnError()
    {
        await OnError.InvokeAsync();

        // The fallback is another image being fetched rather than the end of this one, so the state
        // stays at loading and only the source changes. It is tried once: a fallback that fails as
        // well would otherwise be asked for again by its own error, forever.
        if (_fallbackApplied is false &&
            FallbackSrc.HasValue() &&
            string.Equals(FallbackSrc, _src, StringComparison.Ordinal) is false)
        {
            _fallbackApplied = true;
            _src = FallbackSrc;

            await SetLoadingStateAsync(BitImageState.Loading, forceRender: true);

            return;
        }

        await SetLoadingStateAsync(BitImageState.Error);
    }

    private async Task HandleOnLoad()
    {
        await OnLoad.InvokeAsync();

        await SetLoadingStateAsync(BitImageState.Loaded);
    }

    private async Task SetLoadingStateAsync(BitImageState state, bool forceRender = false)
    {
        var changed = _loadingState != state;

        _loadingState = state;

        // A state set from outside a DOM event - the fallback swap, ReloadAsync - has nothing rendering
        // it, and the callback below reaches the page rather than this component, so the render is asked
        // for here.
        if (forceRender)
        {
            StateHasChanged();
        }

        if (changed)
        {
            await OnLoadingStateChange.InvokeAsync(state);
        }
    }
}
