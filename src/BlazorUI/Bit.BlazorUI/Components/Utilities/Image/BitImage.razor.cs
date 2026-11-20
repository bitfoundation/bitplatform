using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// An image is a graphic representation of something (e.g photo or illustration).
/// </summary>
/// <remarks>
/// The component is an img inside a frame, and nearly everything it offers is about the relationship
/// between the two. The frame takes the size - <see cref="Width"/>, <see cref="Height"/>,
/// <see cref="AspectRatio"/>, <see cref="MaximizeFrame"/>, <see cref="Fluid"/> - and the shape - <see cref="Rounded"/>,
/// <see cref="Circular"/>, <see cref="Bordered"/> - and clips whatever falls outside it, while
/// <see cref="ImageFit"/> and <see cref="ImagePosition"/> decide what the image does inside it once
/// the two turn out to be different shapes.
/// <br />
/// The second half is what happens before the image is there. The component follows the browser's own
/// load and error events through <see cref="BitImageState"/>, and the state decides what is on screen:
/// the image is hidden until it has loaded (unless <see cref="StartVisible"/> says otherwise) and
/// fades in with <see cref="FadeIn"/>, a <see cref="PlaceholderSrc"/> holds the frame meanwhile, a
/// <see cref="LoadingTemplate"/> and an <see cref="ErrorTemplate"/> stand in its place, and a
/// <see cref="FallbackSrc"/> is tried once before the error state is reached at all. While the image
/// is hidden its <see cref="Alt"/> is still announced, so a screen reader is never left with less than
/// the frame. A prerendered or a statically rendered page has no load event to follow, so there all of
/// that starts with the first render in the browser: until then the image is left to the browser and
/// shown while it loads, with its <see cref="PlaceholderSrc"/> under it rather than over it.
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
    /// <summary>
    /// The @keyframes the placeholder fades out with (BitImage.scss), whose end is what takes it out of the DOM.
    /// </summary>
    private const string PlaceholderFadeOutAnimation = "bit-img-fade-out";

    /// <summary>
    /// The classes of what is shown in one color scheme only, where a <see cref="DarkSrc"/> renders an img for
    /// each (BitImage.scss).
    /// </summary>
    private const string LightSchemeClass = "bit-img-lsc";
    private const string DarkSchemeClass = "bit-img-dsc";

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
    /// The source the img of the dark scheme actually renders, which is <see cref="DarkSrc"/> until an error
    /// swaps in the <see cref="FallbackSrc"/>, and whether it has; null while there is no <see cref="DarkSrc"/>
    /// nor <see cref="DarkSrcset"/>, which renders no second img at all.
    /// </summary>
    private string? _darkSrc;
    private bool _darkFallbackApplied;

    /// <summary>
    /// Whether the img of one scheme has failed for good - its fallback included - while there is a second img
    /// (see <see cref="StateOf"/>): the other one may well be on screen, so the failure is that image's alone.
    /// </summary>
    private bool _lightFailed;
    private bool _darkFailed;

    /// <summary>
    /// Whether the sources of the img of either scheme have changed in the parameters being set; see
    /// <see cref="OnParametersSetAsync"/>, which decides what that changes once every parameter is in.
    /// </summary>
    private bool _lightSourceChanged;
    private bool _darkSourceChanged;

    /// <summary>
    /// Whether a parameter change has moved the state back to loading without anyone being told yet;
    /// see <see cref="OnParametersSetAsync"/>.
    /// </summary>
    private bool _stateChangePending;

    /// <summary>
    /// Whether the placeholder of an image that fades in has finished fading out, which is what takes it
    /// out of the DOM rather than leaving it laid over the image, invisible, for the life of the component.
    /// </summary>
    private bool _placeholderFaded;

    /// <summary>
    /// The animations that have started on the placeholder since it began to leave and not yet ended - its fade, or
    /// whatever a restyle of the leaving placeholder runs in its place - the last of which ending is what lets it go.
    /// Counted per name, since one name can run more than once at a time (<c>animation: fade 1s, fade 2s</c>).
    /// </summary>
    private readonly Dictionary<string, int> _placeholderLeaveAnimations = [];

    /// <summary>
    /// Whether the image is the browser's until the first render in the browser takes it over (see
    /// <see cref="_isLeftToBrowser"/>): a prerendered or a statically rendered one, which never gets that far, and
    /// the one of the interactive render that replaces a prerendered one (see <see cref="BitPrerender"/>). An image
    /// of a page that was never prerendered is the component's from the start, so it costs no look at the browser.
    /// </summary>
    private bool _awaitsTakeOver;

    /// <summary>
    /// Whether the browser was already painting the image when the component took it over (see
    /// <see cref="TakeOverFromBrowserAsync"/>), which leaves it to the browser until its load event rather than taking
    /// pixels already on screen away again.
    /// </summary>
    private bool _isKeptByBrowser;

    /// <summary>
    /// Whether the image arrived while the browser was already showing it as its own (see
    /// <see cref="_isLeftToBrowser"/>), which leaves it nothing to fade in from and its placeholder nothing to
    /// cross-fade with.
    /// </summary>
    private bool _arrivedOnScreen;

    /// <summary>
    /// The key of the img element. Changing it replaces the element rather than patching it, which is
    /// the only way to make the browser fetch a source it already has an answer for - see
    /// <see cref="ReloadAsync"/>. The img of the dark scheme is keyed off the same count (see
    /// <see cref="_darkReloadKey"/>).
    /// </summary>
    private int _reloadKey;

    private ElementReference _imageElement;

    private ElementReference _darkImageElement;

    /// <summary>
    /// Whether the img element currently on screen has the Space key suppressed on it, and which
    /// <see cref="_reloadKey"/> that element belongs to - a reload replaces the element, and the
    /// listener goes away with the one it was put on. The img of the dark scheme keeps a flag of its own.
    /// </summary>
    private bool _preventKeysRegistered;
    private bool _darkPreventKeysRegistered;
    private int _preventKeysReloadKey;

    private bool _isClickable => Disabled is false && OnClick.HasDelegate;

    // The two imgs are siblings, so they are keyed apart: the dark one counts down from -1 as the other counts up from 0.
    private int _darkReloadKey => -1 - _reloadKey;

    // Whether a second img is rendered for the dark scheme.
    private bool _hasDarkImage => DarkSrc.HasValue() || DarkSrcset.HasValue();

    // Whether the placeholder of an image that fades in is on its way out.
    private bool _isPlaceholderLeaving => IsPlaceholderLeaving(_loadingState);

    // Until a prerendered component has rendered in the browser the image is left to the browser, shown while it
    // loads the way an img with nothing around it is. That is all a prerendered page shows before it is interactive and all
    // a statically rendered one ever shows, and neither has a load event coming to take a hidden image out of
    // hiding: nothing attaches a handler to their img at all. The interactive render that replaces a prerendered
    // one renders it the same way, so the hand-over changes nothing on screen, and the first render in the
    // browser is where the component takes the image over (see TakeOverFromBrowserAsync) - unless the browser is
    // already painting it by then, which keeps it the browser's until it has finished. A page that was never
    // prerendered has nothing on screen to keep, so its image is the component's from its first render. An img
    // with nothing to load has nothing for the browser to show either, so it is hidden from the start as it always is.
    private bool _isLeftToBrowser => (_awaitsTakeOver || _isKeptByBrowser) &&
                                     _loadingState is BitImageState.Loading &&
                                     (_src.HasValue() || Srcset.HasValue() || _hasSources || _hasDarkImage);

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
    /// The state the img of one scheme is shown by: the state of the component, unless that image has failed
    /// for good on its own while the other one is there (see <see cref="HandleOnErrorAsync"/>). Without a
    /// <see cref="DarkSrc"/> it is the state of the component for both, there being one img.
    /// </summary>
    private BitImageState StateOf(bool dark) => (dark ? _darkFailed : _lightFailed) ? BitImageState.Error : _loadingState;

    // The placeholder stands in for an image that is not on screen, which is as true of one that has
    // failed as of one still on its way: it is taken away by the image arriving rather than by time.
    // An image that fades in keeps it through the fade, fading it out as the image fades in over it -
    // taken away at once, the frame would drop to its background for the length of the fade - and lets it
    // go once that fade has ended.
    private bool IsPlaceholderShown(BitImageState state) => PlaceholderSrc.HasValue() && (state is not BitImageState.Loaded || IsPlaceholderLeaving(state));

    private bool IsPlaceholderLeaving(BitImageState state) => state is BitImageState.Loaded && FadeIn && _placeholderFaded is false;

    // Whether the img element is on screen, which is also whether its alt is in the accessibility tree.
    private bool IsImageVisible(BitImageState state) => state is BitImageState.Loaded ||
                                                        (state is BitImageState.Loading && (StartVisible || _isLeftToBrowser));

    private bool IsLoadingTemplateShown(BitImageState state) => state is BitImageState.Loading && IsImageVisible(state) is false && LoadingTemplate is not null;

    private bool IsErrorTemplateShown(BitImageState state) => state is BitImageState.Error && ErrorTemplate is not null;

    // The text alternative announced in place of an image that is not on screen; see the .bit-img-alt rule.
    // A template on screen speaks for itself, and a decorative image has nothing to announce. It is read
    // the way GetImageAttributes writes the img: a parameter first, then the attribute of the same name a
    // page put in ImageAttributes.
    private bool IsHiddenImageAnnounced(BitImageState state) => IsImageVisible(state) is false &&
                                                                IsLoadingTemplateShown(state) is false &&
                                                                IsErrorTemplateShown(state) is false &&
                                                                GetHiddenImageName() is not null;

    private string? GetHiddenImageName() => FirstValue(AriaLabel, GetImageAttribute("aria-label"), Alt, GetImageAttribute("alt"));

    /// <summary>
    /// The renders of one part of the frame - the placeholder, a template, the hidden image's alt - each with the
    /// scheme it is shown in and the state it is rendered by.
    /// </summary>
    /// <remarks>
    /// With one img that is the part once, for both schemes, while <paramref name="isShown"/> holds for the state
    /// of the component. With an img for each scheme the two can differ - one image has failed while the other is
    /// there - and a part shown for one of them only is rendered for its scheme only (the scheme classes of
    /// BitImage.scss), as is one shown for both in two different states. Rendered for both in the same state, it
    /// is rendered once, for both, as without a <see cref="DarkSrc"/>.
    /// </remarks>
    private IEnumerable<(string? Scheme, BitImageState State)> GetSchemeParts(Func<BitImageState, bool> isShown)
    {
        var light = StateOf(dark: false);
        var lightShown = isShown(light);

        if (_hasDarkImage is false)
        {
            if (lightShown) yield return (null, light);

            yield break;
        }

        var dark = StateOf(dark: true);
        var darkShown = isShown(dark);

        if (lightShown && darkShown && light == dark)
        {
            yield return (null, light);

            yield break;
        }

        if (lightShown) yield return (LightSchemeClass, light);

        if (darkShown) yield return (DarkSchemeClass, dark);
    }

    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private IServiceProvider _services { get; set; } = default!;



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
    /// How the shape of the image compares to the shape of its frame.
    /// </summary>
    /// <remarks>
    /// Only <see cref="BitImageFit.CenterCover"/> and <see cref="BitImageFit.CenterContain"/> read it, to
    /// know which axis to scale along: <see cref="BitImageCover.Landscape"/> for an image proportionally
    /// wider than its frame, <see cref="BitImageCover.Portrait"/> - the default - for a taller one.
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
    /// The src of the image shown in place of <see cref="Src"/> while the color scheme is dark.
    /// </summary>
    /// <remarks>
    /// This is the picture drawn for the other scheme - a logo in light ink, a screenshot taken in the dark
    /// theme. It is rendered as a second img beside the one of <see cref="Src"/>, and which of the two is on
    /// screen is decided in CSS, off the bit-theme attribute the theme script keeps current: the scheme is the
    /// one of the nearest element carrying it - the document's, or a scoped region's - and a theme whose name
    /// ends in "dark" is the dark one. So it is right in prerendered and statically rendered markup too, and it
    /// follows every change of the theme, the operating system's included, without a render. The img of the
    /// other scheme is taken out of the layout and so out of the accessibility tree, and every other parameter -
    /// the alt, the size, the fit, the loading attributes, the click - applies to both.
    /// <br />
    /// A browser fetches an img taken out of the layout all the same unless it is loaded lazily, so an eagerly
    /// loaded image (the default) costs the two downloads; with <see cref="Loading"/> set to
    /// <see cref="BitImageLoading.Lazy"/> only the image of the scheme on screen is fetched, and the other one
    /// once the scheme changes - which is what the trade suits wherever the image is not on the first screen.
    /// <br />
    /// Each image falls back to the <see cref="FallbackSrc"/> on its own, and the
    /// <see cref="BitImageState"/>, the templates, the placeholder and the callbacks follow both: the image of
    /// either scheme arriving loads the component, and one that fails while the other has arrived is shown as
    /// failed - its <see cref="ErrorTemplate"/>, its placeholder or its alt - in its own scheme only. Changing
    /// the source of one scheme alone leaves the state as it is, since the image on screen may well be the
    /// other one, which has nothing new to report. The <see cref="Sources"/> are the img of <see cref="Src"/>'s
    /// alone; <see cref="Sizes"/> is read by both.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetDarkSrc))]
    public string? DarkSrc { get; set; }

    /// <summary>
    /// The set of image sources the browser may choose from while the color scheme is dark, with their width
    /// or density descriptors - the <see cref="Srcset"/> of the <see cref="DarkSrc"/>.
    /// </summary>
    /// <remarks>
    /// It is chosen against the same <see cref="Sizes"/> as the <see cref="Srcset"/>. See <see cref="DarkSrc"/>
    /// for how the image of the scheme on screen is picked.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetDarkSrc))]
    public string? DarkSrcset { get; set; }

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
    /// rendered, so it is the arrival of the image that is animated. A <see cref="PlaceholderSrc"/>
    /// fades out over the same time, so the two cross-fade, and is removed once it has. At the theme's
    /// pace the fade collapses to nothing under prefers-reduced-motion unless
    /// <see cref="BitComponentBase.ForceAnimation"/> says otherwise; a pace a page sets through the
    /// --bit-Image-fade-duration variable is the page's own.
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
    [Parameter, CallOnSet(nameof(OnSetFallbackSrc))]
    public string? FallbackSrc { get; set; }

    /// <summary>
    /// Hints the browser at the priority this image is fetched with, relative to the other resources of the page.
    /// </summary>
    [Parameter] public BitImageFetchPriority? FetchPriority { get; set; }

    /// <summary>
    /// Keeps the frame from growing wider than its container, scaling the image down with it rather than
    /// cropping it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Without it, an image larger than the column it sits in is clipped by its own frame, which is what a
    /// photograph from a content feed does on a phone. The frame keeps its <see cref="Width"/> and
    /// <see cref="AspectRatio"/> up to the width of the container and shrinks below it, and the image is
    /// never enlarged past its natural size - the responsive image of a stylesheet's img { max-width: 100% }.
    /// A frame given a <see cref="Height"/> keeps it, and the image is contained in the narrower frame
    /// rather than squeezed out of its shape; an explicit <see cref="ImageFit"/> still decides otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Fluid { get; set; }

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
    /// <br />
    /// In a frame that has a size of its own - a <see cref="Height"/>, an <see cref="AspectRatio"/>, a
    /// <see cref="MaximizeFrame"/> - it fills that size, so a skeleton given a height of 100% holds exactly
    /// the room the image will take. So does the <see cref="ErrorTemplate"/>.
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
    /// <br />
    /// The frame shades under the pointer and under the press with a tint of the text color, so the
    /// image reads as a control even without a <see cref="Shadow"/> to lift.
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
    /// Renders a shadow under the frame of the image, lifting it off the surface it sits on.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The shadow is the theme's card elevation, so it is the same lift every other raised surface of
    /// the library has rather than a value written here. A clickable image (one with an
    /// <see cref="OnClick"/>) lifts further to the card's hover elevation under the pointer, and settles
    /// back while pressed, the way a clickable card does.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Shadow { get; set; }

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
    /// <br />
    /// An image with nothing to load at all stays in the <see cref="BitImageState.Loading"/> state, since a
    /// source not given yet is most often one still on its way - a URL the page is fetching - rather than
    /// one that will never come. A picture that may be missing for good is what <see cref="FallbackSrc"/> is for.
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
    /// size and the shape. Where a <see cref="DarkSrc"/> is given this is the img of <see cref="Src"/>, which
    /// the dark scheme takes out of the layout in favor of a second img.
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
    /// fail on an element that is not there. Where a <see cref="DarkSrc"/> is given, it is the img of the
    /// scheme on screen that takes the focus: the browser focuses nothing that is out of the layout, so both
    /// are asked, and only that one answers.
    /// </remarks>
    public ValueTask FocusAsync() => FocusImagesAsync(null);

    /// <summary>
    /// Gives the browser focus to the img element of the component.
    /// </summary>
    /// <param name="preventScroll">A Boolean value indicating whether or not the browser should scroll
    /// the document to bring the newly-focused element into view. A value of false for preventScroll (the default)
    /// means that the browser will scroll the element into view after focusing it.
    /// If preventScroll is set to true, no scrolling will occur.</param>
    /// <inheritdoc cref="FocusAsync()" path="/remarks"/>
    public ValueTask FocusAsync(bool preventScroll) => FocusImagesAsync(preventScroll);

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
        _lightFailed = false;
        _src = Src.HasValue() ? Src : FallbackSrc;

        if (_hasDarkImage)
        {
            _darkFallbackApplied = false;
            _darkFailed = false;
            _darkSrc = DarkSrc.HasValue() ? DarkSrc : FallbackSrc;
        }

        return SetLoadingStateAsync(BitImageState.Loading, forceRender: true);
    }



    protected override string RootElementClass => "bit-img";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => MaximizeFrame ? "bit-img-max" : string.Empty);

        ClassBuilder.Register(() => Fluid ? "bit-img-flu" : string.Empty);

        // The centered fits are placed by the frame rather than by the image: an image centered by
        // taking it out of the flow would leave the frame with nothing to take a size from, and a frame
        // that is sized by its content - a flex or grid item, an inline-block - would then collapse to
        // nothing and clip the image away altogether.
        ClassBuilder.Register(() => ImageFit is BitImageFit.Center or BitImageFit.CenterContain or BitImageFit.CenterCover
                                    ? "bit-img-cen"
                                    : string.Empty);

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

    protected override void OnInitialized()
    {
        _awaitsTakeOver = BitPrerender.IsHandOver(_services, IsStaticRender(_js));

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        CascadingParameters?.UpdateParameters(this);

        // A FallbackSrc that arrived through the cascade was assigned without the OnSetSrc a parameter gets,
        // so a change to it is noticed here instead: wherever the fallback is the source on screen - there is
        // no Src of its own, or the Src has already failed over to it - a new one is a new image, with the
        // same return to the loading state, and the same report of it, that a new FallbackSrc parameter has.
        var expectedSrc = _fallbackApplied || Src.HasNoValue() ? FallbackSrc : Src;

        if (string.Equals(_src, expectedSrc, StringComparison.Ordinal) is false)
        {
            _lightSourceChanged = true;
        }

        if (_hasDarkImage)
        {
            var expectedDarkSrc = _darkFallbackApplied || DarkSrc.HasNoValue() ? FallbackSrc : DarkSrc;

            if (string.Equals(_darkSrc, expectedDarkSrc, StringComparison.Ordinal) is false)
            {
                _darkSourceChanged = true;
            }
        }
        else
        {
            RemoveDarkImage();
        }

        if (_lightSourceChanged || _darkSourceChanged)
        {
            ApplySourceChanges();
        }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await TakeOverFromBrowserAsync();
        }

        // Space scrolls the page by default, and an image that answers Space is a control the reader
        // pressed it on rather than a place to scroll from. A Blazor keydown handler cannot decide that
        // per key - @onkeydown:preventDefault is evaluated at render time, so a flag set from the
        // handler lags a keystroke behind and would swallow the Tab that follows - so it is stopped on
        // a listener of the browser's own, registered for an image that has a click handler and emptied
        // again for one that has lost it, since the listener stays on the element and reads the key list
        // on every event. It is the img rather than the frame that carries it: the img is what is
        // focused, and it holds nothing that the suppression could be taken from.
        var interactive = OnClick.HasDelegate;

        // A reload replaces the img element rather than patching it, and the listener stays behind on
        // the element that was taken away, so a new one starts as nothing registered.
        if (_reloadKey != _preventKeysReloadKey)
        {
            _preventKeysReloadKey = _reloadKey;
            _preventKeysRegistered = false;
            _darkPreventKeysRegistered = false;
        }

        // The img of the dark scheme goes away with its sources, and the listener with it.
        if (_hasDarkImage is false)
        {
            _darkPreventKeysRegistered = false;
        }

        if (interactive != _preventKeysRegistered && await RegisterPreventKeysAsync(_imageElement, interactive))
        {
            // Only a call that landed counts as registered: flagging it beforehand would leave a failed
            // one believing the listener is installed, and the guard above would never let it be tried
            // again.
            _preventKeysRegistered = interactive;
        }

        if (_hasDarkImage && interactive != _darkPreventKeysRegistered && await RegisterPreventKeysAsync(_darkImageElement, interactive))
        {
            _darkPreventKeysRegistered = interactive;
        }
    }

    private async Task<bool> RegisterPreventKeysAsync(ElementReference element, bool interactive)
    {
        try
        {
            await _js.BitUtilsRegisterPreventKeys(element, interactive ? [" ", "Spacebar"] : []);

            return true;
        }
        catch (JSDisconnectedException) { return false; } // the circuit is gone, nothing to register
        catch (JSException) { return false; } // a JS-side failure here only costs the page-scroll prevention
    }

    /// <summary>
    /// Focuses the img of the scheme on screen: the img of the other one is out of the layout, which the
    /// browser focuses nothing in, so both are asked and only the one on screen answers. The dark one is asked
    /// first, so the img of <see cref="Src"/> - the one there is without a <see cref="DarkSrc"/> - is asked last
    /// and has the last word wherever no stylesheet decides between the two.
    /// </summary>
    private async ValueTask FocusImagesAsync(bool? preventScroll)
    {
        if (_hasDarkImage && _darkImageElement.Context is not null)
        {
            await (preventScroll.HasValue ? _darkImageElement.FocusAsync(preventScroll.Value) : _darkImageElement.FocusAsync());
        }

        if (_imageElement.Context is null) return;

        await (preventScroll.HasValue ? _imageElement.FocusAsync(preventScroll.Value) : _imageElement.FocusAsync());
    }



    // A change of the sources is only noted while the parameters are being assigned, and taken in once they all are
    // (see ApplySourceChanges): what it changes depends on the sources of the other scheme, which may not have been
    // assigned yet. The fallback is a source of both.
    private void OnSetSrc() => _lightSourceChanged = true;

    private void OnSetDarkSrc() => _darkSourceChanged = true;

    private void OnSetFallbackSrc()
    {
        _lightSourceChanged = true;
        _darkSourceChanged = true;
    }

    /// <summary>
    /// A new source is a new image, so whatever the previous one ended up as is no longer the answer:
    /// the state returns to loading and the fallback is available again.
    /// </summary>
    /// <remarks>
    /// Where a <see cref="DarkSrc"/> renders a second img, that is so of each img whose sources changed, but the
    /// state only returns to loading once the sources of both have: the image on screen may well be the one
    /// whose sources stayed as they were, which has no load event coming to take it out of hiding again.
    /// </remarks>
    private void ApplySourceChanges()
    {
        var renewsAll = _lightSourceChanged && (_darkSourceChanged || _hasDarkImage is false);

        if (_lightSourceChanged)
        {
            _fallbackApplied = false;
            _lightFailed = false;
            _src = Src.HasValue() ? Src : FallbackSrc;
        }

        if (_darkSourceChanged && _hasDarkImage)
        {
            _darkFallbackApplied = false;
            _darkFailed = false;
            _darkSrc = DarkSrc.HasValue() ? DarkSrc : FallbackSrc;
        }

        _lightSourceChanged = false;
        _darkSourceChanged = false;

        if (renewsAll is false) return;

        // What the browser was painting was the previous source; the new one is the component's to show - even
        // while the browser is still being asked about the previous one (see TakeOverFromBrowserAsync), whose answer
        // would otherwise leave the new source to the browser until it lands, and then take it back. The source the
        // component starts with is set before its first render, and is the one the hand-over is about.
        _isKeptByBrowser = false;
        if (IsRendered) _awaitsTakeOver = false;

        if (_loadingState == BitImageState.Loading) return;

        _loadingState = BitImageState.Loading;
        _arrivedOnScreen = false;
        _stateChangePending = true;
    }

    /// <summary>
    /// Forgets the img of the dark scheme once there is no <see cref="DarkSrc"/> nor <see cref="DarkSrcset"/>
    /// to render it for. The img of <see cref="Src"/> is then the image, so a failure that was its own alone is
    /// the state of the component.
    /// </summary>
    private void RemoveDarkImage()
    {
        _darkSrc = null;
        _darkFallbackApplied = false;
        _darkFailed = false;
        _darkSourceChanged = false;

        if (_lightFailed is false) return;

        _lightFailed = false;

        if (_loadingState is BitImageState.Error) return;

        _loadingState = BitImageState.Error;
        _stateChangePending = true;
    }

    /// <summary>
    /// Takes the image over from the browser, which has had it to itself until the first render in the browser
    /// (see <see cref="_isLeftToBrowser"/>).
    /// </summary>
    /// <remarks>
    /// An image the browser has already finished is kept on screen as it is: it has arrived, and hiding it only to
    /// show it again on its load event would be the very flash the hand-over is there to avoid. It is the img the
    /// browser is asked about rather than its load event that is waited for, since a cached image is complete the
    /// moment its element is inserted. The load event still follows, and reports the load through
    /// <see cref="OnLoad"/> as it always does; the state it would move to is already reached. An image the browser
    /// is still painting stays the browser's until that event, for the same reason. Only an image with nothing of it
    /// on screen yet goes back to the loading state as the component shows it - hidden, behind its
    /// <see cref="LoadingTemplate"/> or its <see cref="PlaceholderSrc"/> - to be revealed by that event. A component
    /// of a page that was never prerendered never gets here: its image was the component's from its first render.
    /// </remarks>
    private async Task TakeOverFromBrowserAsync()
    {
        if (_isLeftToBrowser is false)
        {
            _awaitsTakeOver = false;
            return;
        }

        var src = _src;
        var reloadKey = _reloadKey;
        var progress = 0;

        try
        {
            progress = await _js.BitUtilsGetImageProgress(_imageElement);

            // With a second img for the dark scheme it is whichever of the two the browser got furthest with. Only
            // the stylesheet knows which one is on screen, and keeping the image the browser's while the other one
            // finishes takes nothing off the screen: it was being shown as the browser's all along.
            if (_hasDarkImage)
            {
                progress = Math.Max(progress, await _js.BitUtilsGetImageProgress(_darkImageElement));
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, there is nothing left to show
        catch (JSException) { } // an image that cannot be asked about is shown the way the component shows any other

        _awaitsTakeOver = false;

        // A load or an error event, a new source or a reload that landed while the browser was being asked is
        // newer than its answer, which is then about an image that is no longer the one on screen.
        if (progress > 0 && _loadingState is BitImageState.Loading && _reloadKey == reloadKey && string.Equals(_src, src, StringComparison.Ordinal))
        {
            _isKeptByBrowser = true;

            // Reached while the image is still the browser's own, which is what keeps the fade and the
            // placeholder out of the arrival of an image that was on screen all along.
            if (progress == 2)
            {
                await SetLoadingStateAsync(BitImageState.Loaded);
            }
        }

        StateHasChanged();
    }

    /// <summary>
    /// The value of an attribute a page put in <see cref="ImageAttributes"/>, found by its name the way
    /// HTML finds it, regardless of case.
    /// </summary>
    private string? GetImageAttribute(string name)
    {
        foreach (var attribute in ImageAttributes)
        {
            if (string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return attribute.Value?.ToString();
            }
        }

        return null;
    }

    private static string? FirstValue(params string?[] values)
    {
        foreach (var value in values)
        {
            if (value.HasValue()) return value;
        }

        return null;
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
    /// <br />
    /// The sources come last, after everything that says how they are to be fetched (loading,
    /// fetchpriority, crossorigin, referrerpolicy): the renderer writes the attributes of a new img one by
    /// one, in this order, and a browser may start the request the moment the src is written - at once
    /// when the URL is already in its memory cache, as the image a prerendered page deferred is when the
    /// interactive render replaces it. Written ahead of the loading attribute, a lazy image far off screen
    /// would then be fetched as an eager one.
    /// <br />
    /// The img of the dark scheme (<paramref name="dark"/>) is written the same way but for its sources, which
    /// are <see cref="DarkSrc"/> and <see cref="DarkSrcset"/> alone - a src or a srcset put in ImageAttributes is
    /// the picture of the other scheme - and for an id, which names one element of the page and so stays on the
    /// img of <see cref="Src"/>.
    /// </remarks>
    private Dictionary<string, object> GetImageAttributes(bool dark = false)
    {
        Dictionary<string, object> attributes = new(ImageAttributes.Count + 14, StringComparer.OrdinalIgnoreCase);

        object? splattedSrc = null, splattedSrcset = null, splattedSizes = null;

        foreach (var attribute in ImageAttributes)
        {
            if (string.Equals(attribute.Key, "src", StringComparison.OrdinalIgnoreCase))
            {
                splattedSrc = dark ? null : attribute.Value;
            }
            else if (string.Equals(attribute.Key, "srcset", StringComparison.OrdinalIgnoreCase))
            {
                splattedSrcset = dark ? null : attribute.Value;
            }
            else if (string.Equals(attribute.Key, "sizes", StringComparison.OrdinalIgnoreCase))
            {
                splattedSizes = attribute.Value;
            }
            else if (dark && string.Equals(attribute.Key, "id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            else
            {
                attributes[attribute.Key] = attribute.Value;
            }
        }

        // An image is never left without an alt: one that has no text of its own is decorative, which
        // is what an empty alt says, and is skipped by a screen reader rather than read out as a URL.
        if (Alt is not null || attributes.ContainsKey("alt") is false)
        {
            attributes["alt"] = Alt ?? string.Empty;
        }

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
        Set(attributes, "referrerpolicy", GetReferrerPolicy());
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

            if (Disabled)
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
                                                : null, dark);

        // Last, in the order a browser reads them in: the sizes the srcset is chosen against, the srcset,
        // and the src. A parameter wins over what the page put in ImageAttributes, as everywhere above.
        SetOrKeep(attributes, "sizes", Sizes, splattedSizes);
        SetOrKeep(attributes, "srcset", dark ? DarkSrcset : Srcset, splattedSrcset);
        SetOrKeep(attributes, "src", dark ? _darkSrc : _src, splattedSrc);

        return attributes;

        static void SetOrKeep(Dictionary<string, object> attributes, string name, string? value, object? splatted)
        {
            if (value.HasValue())
            {
                attributes[name] = value!;
            }
            else if (splatted is not null)
            {
                attributes[name] = splatted;
            }
        }

        static void Set(Dictionary<string, object> attributes, string name, string? value)
        {
            if (value.HasNoValue()) return;

            attributes[name] = value!;
        }
    }

    private string? GetReferrerPolicy() => ReferrerPolicy switch
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
    };

    private string? GetPlaceholderReferrerPolicy() => GetReferrerPolicy() ?? GetImageAttribute("referrerpolicy");

    private string GetPlaceholderClasses(BitImageState state, string? scheme)
    {
        var className = IsPlaceholderLeaving(state) ? "bit-img-plc bit-img-pfo" : "bit-img-plc";

        return JoinClasses(JoinClasses(className, scheme), Classes?.Placeholder);
    }

    // The class of a part of the frame: its own, then the scheme it is rendered for (see GetSchemeParts), then the
    // page's.
    private string GetTemplateClasses(string? scheme, string? classes) => JoinClasses(JoinClasses("bit-img-tpl", scheme), classes);

    private static string JoinClasses(string className, string? other) => other.HasValue() ? $"{className} {other}" : className;

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

    private string GetImageClasses(string? splattedClass = null, bool dark = false)
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

        // A fluid frame scales an image left to its default fit down with it rather than cropping it. An
        // explicit fit is a decision about this image, so it is left to that fit alone.
        if (ImageFit.HasValue is false && Fluid)
        {
            className.Append(" bit-img-fli");
        }

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

        // Where there are two imgs each one is told apart by the scheme it is shown in (BitImage.scss).
        if (_hasDarkImage)
        {
            className.Append(dark ? $" {DarkSchemeClass}" : $" {LightSchemeClass}");
        }

        if (IsImageVisible(StateOf(dark)))
        {
            className.Append(" bit-img-vis");

            // The fade belongs to the moment the image becomes visible rather than to the mounting of
            // the component: the class arrives with the one above it, which is the render the load
            // event caused, so the animation starts where the image does. An image the browser is showing
            // as its own has not arrived in that sense - a prerendered one would fade in on page load and
            // again when the interactive render replaces it - and neither has one that was on screen
            // before the component took it over.
            if (FadeIn && _isLeftToBrowser is false && _arrivedOnScreen is false)
            {
                className.Append(" bit-img-fde");
            }

            // The image the browser is showing as its own is lifted over the placeholder, which stays under
            // it, so the image paints over the blur as it arrives rather than waiting under a placeholder
            // that nothing on a static page would ever take away.
            if (_isLeftToBrowser)
            {
                className.Append(" bit-img-nat");
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
        if (Disabled) return;

        await OnClick.InvokeAsync(e);
    }

    // The Enter key of a button activates it on the way down, and the Space key on the way up. Nothing
    // is rendered as focusable at all unless there is a click handler to answer, so a page that never
    // makes the image clickable pays for neither of these. The Space that activates on the way up is
    // kept from scrolling the page on the way down by the listener registered in OnAfterRenderAsync.
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

    private Task HandleOnError() => HandleOnErrorAsync(dark: false);

    private Task HandleOnDarkError() => HandleOnErrorAsync(dark: true);

    private async Task HandleOnErrorAsync(bool dark)
    {
        await OnError.InvokeAsync();

        // The image of a DarkSrc that has already arrived is the other one: with two imgs, a failure that comes after
        // it is the failing image's alone, and it takes nothing away from the one that is there.
        var otherArrived = _hasDarkImage && _loadingState is BitImageState.Loaded;

        // The fallback is another image being fetched rather than the end of this one, so the state
        // stays at loading and only the source changes. It is tried once: a fallback that fails as
        // well would otherwise be asked for again by its own error, forever.
        if ((dark ? _darkFallbackApplied : _fallbackApplied) is false &&
            FallbackSrc.HasValue() &&
            string.Equals(FallbackSrc, dark ? _darkSrc : _src, StringComparison.Ordinal) is false)
        {
            if (dark)
            {
                _darkFallbackApplied = true;
                _darkSrc = FallbackSrc;
            }
            else
            {
                _fallbackApplied = true;
                _src = FallbackSrc;
            }

            if (otherArrived) return;

            // The fallback is a new image, the component's to show (see ApplySourceChanges).
            _isKeptByBrowser = false;
            _awaitsTakeOver = false;

            await SetLoadingStateAsync(BitImageState.Loading, forceRender: true);

            return;
        }

        // Kept per image only where there are two of them (see StateOf).
        if (_hasDarkImage)
        {
            if (dark)
            {
                _darkFailed = true;
            }
            else
            {
                _lightFailed = true;
            }

            if (otherArrived) return;
        }

        await SetLoadingStateAsync(BitImageState.Error);
    }

    // The placeholder of an image that fades in is laid over it, fading out, and would otherwise stay there
    // at no opacity - with its blur and its scale still on it - for as long as the component lives, so it
    // is let go the moment its fade has ended. The leaving class replaces whatever the placeholder was animating
    // with the fade, so what starts on it from then on is the fade, or what a restyle of the leaving placeholder runs
    // in its place under another name; it is let go once the last of those has ended or been canceled. An animation
    // that was already running when the leaving started is not waited for. An end that comes with no start seen is
    // taken for the fade when it carries its name or none at all - one fired before the library's script had
    // registered the event's arguments - since a placeholder kept over the image for good is the worse mistake.
    private void HandleOnPlaceholderAnimationStart(BitAnimationEventArgs e)
    {
        if (_isPlaceholderLeaving is false) return;

        var name = e.AnimationName ?? string.Empty;

        _placeholderLeaveAnimations[name] = _placeholderLeaveAnimations.GetValueOrDefault(name) + 1;
    }

    private void HandleOnPlaceholderAnimationEnd(BitAnimationEventArgs e)
    {
        if (_isPlaceholderLeaving is false) return;

        var name = e.AnimationName ?? string.Empty;

        if (_placeholderLeaveAnimations.TryGetValue(name, out var running))
        {
            if (running > 1)
            {
                _placeholderLeaveAnimations[name] = running - 1;
            }
            else
            {
                _placeholderLeaveAnimations.Remove(name);
            }

            if (_placeholderLeaveAnimations.Count > 0) return;
        }
        else if (e.AnimationName.HasValue() && e.AnimationName != PlaceholderFadeOutAnimation)
        {
            return;
        }

        _placeholderFaded = true;
    }

    private Task HandleOnLoad() => HandleOnLoadAsync(dark: false);

    private Task HandleOnDarkLoad() => HandleOnLoadAsync(dark: true);

    // The image of either scheme arriving loads the component: which of the two is on screen is the stylesheet's
    // to know, and a lazily loaded pair only fetches that one.
    private async Task HandleOnLoadAsync(bool dark)
    {
        await OnLoad.InvokeAsync();

        if (dark)
        {
            _darkFailed = false;
        }
        else
        {
            _lightFailed = false;
        }

        await SetLoadingStateAsync(BitImageState.Loaded);
    }

    private async Task SetLoadingStateAsync(BitImageState state, bool forceRender = false)
    {
        var changed = _loadingState != state;

        // Read before the state moves on, since the loading state is part of what leaves the image to the browser.
        var arrivedOnScreen = _isLeftToBrowser;

        // The image the browser kept has now arrived or failed, or another load has begun (a fallback, a reload),
        // which is the component's to show.
        _isKeptByBrowser = false;

        _loadingState = state;

        // Arriving again is a new fade, and so a placeholder to fade out again. Only an arrival counts: a
        // srcset that swaps in another candidate on a resize loads again without ever having left. An image
        // that arrives while the browser is already showing it as its own has been on screen all along, so it
        // has nothing to fade in from, and its placeholder - kept under it until now - is let go at once
        // rather than brought up over it to fade out.
        if (changed)
        {
            _arrivedOnScreen = state is BitImageState.Loaded && arrivedOnScreen;

            if (state is BitImageState.Loaded)
            {
                _placeholderFaded = arrivedOnScreen;
                _placeholderLeaveAnimations.Clear();
            }
        }

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
