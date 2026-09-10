namespace Bit.BlazorUI;

/// <summary>
/// BitFlag is a component that renders the flag image of a country.
/// </summary>
/// <remarks>
/// The country is named in whichever of its five ways the page already has it in - a
/// <see cref="Country"/> out of <see cref="BitCountries"/>, an <see cref="Iso2"/> or <see cref="Iso3"/>
/// code, a dialing <see cref="Code"/> or an English <see cref="Name"/> - and the first of them that
/// resolves picks the flag. The name is read as a page actually writes one: the alternative names, the
/// abbreviations and the spellings of <see cref="BitCountries.FindByName"/> all resolve. A country the
/// table does not carry draws nothing at all rather than a broken image, which is what
/// <see cref="FallbackTemplate"/> is for.
/// <br />
/// What is drawn is a 16 pixel image out of the Extras package, unless <see cref="Emoji"/> asks for the
/// Unicode emoji flag instead - which is text, so it costs no request and stays crisp at any size,
/// where the image is only as sharp as its 16 pixels - or <see cref="Src"/> points the flag at a set of
/// images of the page's own, which falls back to the packaged flag where it fails. The frame around it
/// takes the <see cref="Size"/>, or a <see cref="Width"/>, <see cref="Height"/> and
/// <see cref="AspectRatio"/> of its own, and the shape: <see cref="Rounded"/>, <see cref="Circular"/>,
/// <see cref="Bordered"/> - which is what keeps a mostly white flag off a white surface -
/// <see cref="Shadow"/> and <see cref="Grayscale"/>.
/// <br />
/// To assistive technologies a flag is decorative by default: it nearly always sits beside the name of
/// the very country it stands for, and a second reading of that name is noise. A flag that carries
/// meaning of its own is named with an <see cref="Alt"/>, or with the country's own name through
/// <see cref="AutoAlt"/>, and <see cref="AutoTitle"/> does the same for the tooltip. An
/// <see cref="OnClick"/> turns the whole frame into a button that answers the keyboard as well as the
/// pointer, and grows its target to the 24 pixels WCAG asks of one without growing the flag - which is
/// what a flag standing for a country in a picker needs.
/// <br />
/// A flag stands for a country and never for a language: "Spanish" is not the flag of Spain to the
/// larger part of the world that speaks it, and English belongs to no one flag at all. A language
/// switcher wants the name of the language written in that language; a country, a region and a
/// currency are what this is for.
/// </remarks>
public partial class BitFlag : BitComponentBase
{
    /// <summary>
    /// Where the packaged flag images live, and the shape of their names.
    /// </summary>
    private const string FlagsPath = "_content/Bit.BlazorUI.Extras/flags/";
    private const string FlagsSuffix = "-flat-16.webp";

    /// <summary>
    /// The country the parameters resolved to, worked out once per parameter set rather than on every
    /// read: the flag, its emoji, its name and its tooltip all ask for it.
    /// </summary>
    private BitCountry? _country;

    /// <summary>
    /// The emoji flag to draw, which is null wherever the flag is drawn as an image.
    /// </summary>
    private string? _emoji;

    /// <summary>
    /// The image the flag would draw, before the error of a previous attempt at it is taken into
    /// account. Kept apart from <see cref="_src"/> so that a new source clears an error the old one
    /// ended in.
    /// </summary>
    private string? _srcCandidate;

    /// <summary>
    /// The packaged flag image of the country that resolved, which is what a <see cref="Src"/> of the
    /// page's own falls back to when it fails to load.
    /// </summary>
    private string? _packagedSrc;

    /// <summary>
    /// Whether the image currently pointed at has already failed to load. An image that failed is not
    /// drawn again - the browser would ask for it once per render - and the packaged flag, or the
    /// fallback, stands in its place instead.
    /// </summary>
    private bool _hasError;

    /// <summary>
    /// Whether the packaged flag stood in for a <see cref="Src"/> that failed and then failed itself.
    /// There is nothing left to try after it, so the fallback takes over.
    /// </summary>
    private bool _hasFallbackError;

    /// <summary>
    /// Whether a Space is being held down on the flag itself. A Space activates a button on the way up
    /// rather than on the way down, and only where the same element saw it pressed.
    /// </summary>
    private bool _spacePressed;

    /// <summary>
    /// Whether the browser-side listener that keeps Space from scrolling the page is currently
    /// installed on the root element.
    /// </summary>
    private bool _preventKeysRegistered;

    /// <summary>
    /// The image actually drawn. A <see cref="Src"/> of the page's own that failed to load is stood in
    /// for by the packaged flag of the same country - a set of a page's own that turns out not to
    /// cover a country is better answered with the flag that ships than with nothing at all - and only
    /// once that has failed too, or where there was never a second image to try, is there none.
    /// </summary>
    private string? _src
    {
        get
        {
            if (_hasError is false) return _srcCandidate;

            if (_hasFallbackError) return null;

            return string.Equals(_packagedSrc, _srcCandidate, StringComparison.Ordinal) ? null : _packagedSrc;
        }
    }

    /// <summary>
    /// Whether anything has asked the frame to be a shape of its own rather than whatever fits what is
    /// drawn in it - a corner, a circle, a border, a shadow or a ratio.
    /// </summary>
    private bool _shaped => Rounded || Circular || Bordered || Shadow || AspectRatio.HasValue();

    private string _loading => Loading switch
    {
        BitImageLoading.Eager => "eager",
        _ => "lazy"
    };

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The alternative text of the flag image, which is also the accessible name of the flag.
    /// </summary>
    /// <remarks>
    /// A flag is decorative unless something names it: it nearly always sits beside the name of the
    /// country it stands for, and an image that repeats the text beside it is noise to a screen
    /// reader. Set this where the flag is the only thing saying which country is meant - and where
    /// <see cref="OnClick"/> makes it a button, this is the name of the button, so it says what the
    /// click does rather than what the flag shows.
    /// <br />
    /// <see cref="AutoAlt"/> fills it in with the country's own name instead of it being written out.
    /// </remarks>
    [Parameter] public string? Alt { get; set; }

    /// <summary>
    /// Names the flag to assistive technologies with the full name of the country it resolved to.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is <see cref="Alt"/> written for you, for the flag that stands on its own - in a table
    /// cell, or as the whole of a country picker - rather than beside the name it would repeat. An
    /// <see cref="Alt"/> of the page's own wins over it, and a flag whose country did not resolve has
    /// no name to take and stays decorative.
    /// </remarks>
    [Parameter] public bool AutoAlt { get; set; }

    /// <summary>
    /// Sets the tooltip of the flag to the full name of the country it resolved to.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A tooltip is a pointer affordance rather than an accessible name - it is not read by a screen
    /// reader on its own and never reaches a touch device at all - so a flag that has to be named to
    /// everyone wants <see cref="AutoAlt"/> as well. A <see cref="Title"/> of the page's own wins
    /// over it.
    /// </remarks>
    [Parameter] public bool AutoTitle { get; set; }

    /// <summary>
    /// The aspect ratio of the frame of the flag, as any CSS aspect-ratio value (e.g. "4/3" or "1").
    /// </summary>
    /// <remarks>
    /// The packaged images are square and the frame is square with them, which is the shape a flag has
    /// in a list beside anything else. A set of the page's own pointed at with <see cref="Src"/> is
    /// usually drawn in the proportions of the flags themselves - 4:3 and 3:2 are what the vector sets
    /// of the world ship - and this is what gives them a frame of that shape: the height is the one
    /// that stays and the width follows from the ratio, or the other way round where a
    /// <see cref="Width"/> is what was given. It pairs with <see cref="Fit"/>, which decides what the
    /// image does inside a frame that is not its own shape.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Draws a hairline border around the flag.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A flag that is mostly white - Japan's, Switzerland's, Finland's - disappears into a white
    /// surface without one, which is the reason this exists. The border is drawn inside the frame, so
    /// it costs no layout: a bordered flag is exactly as big as one without.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Bordered { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the flag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitFlagClassStyles? Classes { get; set; }

    /// <summary>
    /// Clips the flag into a circle.
    /// </summary>
    /// <remarks>
    /// The flag images are square, so nothing is cropped away but the corners. It wins over
    /// <see cref="Rounded"/> where both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Circular { get; set; }

    /// <summary>
    /// The dialing code of the country.
    /// </summary>
    /// <remarks>
    /// The code is read the way a telephone number is written rather than as an exact key: "+31",
    /// "00 31" and "31" all reach the Netherlands. Dialing codes are not unique - Canada and the
    /// United States both carry "1" - and the first country of <see cref="BitCountries.All"/> that
    /// carries the code wins, so where the difference matters name the country by its ISO code.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? Code { get; set; }

    /// <summary>
    /// The country to render the flag.
    /// </summary>
    /// <remarks>
    /// This is the fastest of the five ways of naming the country - there is nothing to look up - and
    /// it wins over all of them. It is not restricted to <see cref="BitCountries.All"/> either: a
    /// country of the page's own is rendered from its own <see cref="BitCountry.Iso2"/>, which is what
    /// lets a <see cref="Src"/> of the page's own cover a country the packaged images do not.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitCountry? Country { get; set; }

    /// <summary>
    /// Renders the flag as its Unicode emoji instead of as an image.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The emoji flag is text, so it costs no request at all and stays crisp at any size, where the
    /// packaged image is only as sharp as its 16 pixels. What it looks like is the platform's to
    /// decide, though: the emoji fonts of Apple, Google and the Noto family draw the flags, while
    /// Windows draws the two letters of the country code side by side instead - which is why the image
    /// is what is drawn unless this asks otherwise.
    /// <br />
    /// It is built from the country code rather than looked up, so it also answers for a code the
    /// packaged images do not cover, and it wins over <see cref="Src"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Emoji { get; set; }

    /// <summary>
    /// What to render in place of the flag when there is none to draw.
    /// </summary>
    /// <remarks>
    /// It stands in for a country that resolved to nothing - a code no country of
    /// <see cref="BitCountries.All"/> carries, or none given at all - and for an image that failed to
    /// load. A <see cref="Src"/> of the page's own that failed is stood in for by the packaged flag of
    /// the same country first, so this is what is left once there is no flag to draw at all. Without
    /// one, such a flag draws nothing and leaves an empty frame of its own size.
    /// </remarks>
    [Parameter] public RenderFragment? FallbackTemplate { get; set; }

    /// <summary>
    /// How the flag image is scaled and cropped to fit the frame around it.
    /// </summary>
    /// <remarks>
    /// It only matters where the image and the frame turn out to be different shapes, which is what a
    /// <see cref="Src"/> of the page's own or an <see cref="AspectRatio"/> makes possible: the packaged
    /// images are square and so is the frame they are drawn in. Left unset, the image covers the frame
    /// and whatever falls outside it is cropped, which is what keeps a circular or rounded flag full
    /// of flag rather than of empty space; <see cref="BitImageFit.Contain"/> is the other answer, and
    /// fits the whole flag inside the frame instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitImageFit? Fit { get; set; }

    /// <summary>
    /// Draws the flag in shades of grey.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is how a flag says that the country is not the one in play - an unavailable locale, a
    /// region not shipped to - while staying recognisable. It is a filter over whatever is drawn, so
    /// it applies to the emoji flag as much as to the image.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Grayscale { get; set; }

    /// <summary>
    /// The height of the flag, as any CSS length.
    /// </summary>
    /// <remarks>
    /// It also becomes the size the emoji flag is drawn at, and the width where no
    /// <see cref="Width"/> is set - the flag images are square, so one length is usually all there is
    /// to say. Both of them win over <see cref="Size"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Additional HTML attributes to render on the img element of the flag.
    /// </summary>
    /// <remarks>
    /// The attributes of the component itself land on the frame, which is what a flag is measured,
    /// named and clicked by. This is for the few that belong on the picture instead - a
    /// <c>crossorigin</c> or a <c>referrerpolicy</c> for a <see cref="Src"/> pointing at a CDN, a
    /// <c>fetchpriority</c> for one that has to be fetched before the layout settles. They are written
    /// before everything the flag decides, so the src, the alt and the loading of the flag itself
    /// still win over them - but the two the flag only defaults, the <c>draggable</c> and the
    /// <c>decoding</c>, are left exactly as they are given here.
    /// </remarks>
    [Parameter] public Dictionary<string, object> ImageAttributes { get; set; } = [];

    /// <summary>
    /// The ISO 3166-1 alpha-2 code of the country.
    /// </summary>
    /// <remarks>
    /// It is matched case insensitively against <see cref="BitCountries.All"/>, and a code no country
    /// of the table carries draws nothing rather than asking the network for an image that is not
    /// there. The exceptionally reserved "UK" is answered with the United Kingdom, whose own code is
    /// "GB".
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? Iso2 { get; set; }

    /// <summary>
    /// The ISO 3166-1 alpha-3 code of the country.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Iso3 { get; set; }

    /// <summary>
    /// How the browser should load the flag image.
    /// </summary>
    /// <remarks>
    /// It defaults to loading lazily, which is what a list of two hundred flags in a country picker
    /// wants: only the rows the reader actually scrolls to are ever fetched. A single flag that is on
    /// screen from the first frame is fetched just as eagerly either way - the browser loads a lazy
    /// image already in the viewport at once - so this is only worth setting for a flag whose fetch
    /// has to be started before the layout settles.
    /// </remarks>
    [Parameter] public BitImageLoading? Loading { get; set; }

    /// <summary>
    /// The full English name of the country.
    /// </summary>
    /// <remarks>
    /// It is matched case insensitively against the whole name of each country of
    /// <see cref="BitCountries.All"/>, rather than against part of it. A name a page's own data
    /// spells differently still resolves: the alternative names and the abbreviations a country is as
    /// widely known by are answered too - "Czechia", "Türkiye", "Holland", "UK", "USA" - and so are
    /// the accents, the punctuation and the spacing another source writes it with, so "Curaçao" and
    /// "Guinea-Bissau" reach the countries this table writes as "Curacao" and "Guinea Bissau".
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? Name { get; set; }

    /// <summary>
    /// The callback for when the flag is clicked.
    /// </summary>
    /// <remarks>
    /// Setting it turns the flag into a button: it takes the button role, joins the tab order and
    /// answers Enter and Space as well as the pointer. A button needs a name to be announced by, so
    /// give a clickable flag an <see cref="Alt"/> saying what the click does; without one it falls
    /// back to the name of the country it shows, since "Canada, button" is worth more than "button"
    /// on its own - and a button with neither is left without the role altogether, so that a screen
    /// reader passes over it rather than stopping at a control with nothing to read out.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The callback for when the flag image fails to load.
    /// </summary>
    /// <remarks>
    /// It is the browser's own error event, so it only fires for a flag drawn as an image - the emoji
    /// flag is text and cannot fail. Where a <see cref="Src"/> of the page's own failed and the
    /// packaged flag of the same country stood in for it, this fires once for each of them: the first
    /// says the page's own image is not there, the second that there is nothing left to draw.
    /// </remarks>
    [Parameter] public EventCallback OnError { get; set; }

    /// <summary>
    /// The callback for when the flag image has loaded.
    /// </summary>
    /// <remarks>
    /// It is the browser's own load event, so it only fires for a flag drawn as an image, and it fires
    /// again whenever the image the flag points at changes.
    /// </remarks>
    [Parameter] public EventCallback OnLoad { get; set; }

    /// <summary>
    /// Rounds the corners of the flag.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// <see cref="Circular"/> wins over it where both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Rounded { get; set; }

    /// <summary>
    /// Draws a shadow under the flag.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// It is the card shadow of the theme, so it lifts the flag off the surface the same way every
    /// other raised surface of the library does. The frame clips its contents but never its own
    /// shadow, so the shadow survives the rounded and circular shapes.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Shadow { get; set; }

    /// <summary>
    /// The size of the flag, out of the icon sizes of the theme.
    /// </summary>
    /// <remarks>
    /// Medium is the 16 pixels the packaged images are drawn at and the size a flag has always been,
    /// so it is what a flag with no size set is. The images are raster rather than vector: a large
    /// flag is those same 16 pixels scaled up, which the flat artwork carries well but only so far -
    /// past the sizes of the theme, either the emoji flag or a <see cref="Src"/> of a set of the page's
    /// own stays sharp. <see cref="Width"/> and <see cref="Height"/> win over it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// The url of the image to render instead of the packaged flag image.
    /// </summary>
    /// <remarks>
    /// This is what points the flag at a set of images of the page's own - a vector set that stays
    /// sharp at any size, a set drawn in another style, or one covering a country the packaged images
    /// do not. It is used exactly as given, so the country parameters are then only what names the
    /// flag and fills in its tooltip. A source that fails to load falls back to the packaged flag of
    /// the country that resolved, and then to <see cref="FallbackTemplate"/>, so a set of a page's own
    /// that turns out not to cover a country is answered with the flag that ships rather than with
    /// nothing. Such a set is usually drawn in the proportions of the flags themselves rather than
    /// square, which is what <see cref="AspectRatio"/> and <see cref="Fit"/> are for.
    /// <see cref="Emoji"/> wins over it.
    /// </remarks>
    [Parameter] public string? Src { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the flag.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitFlagClassStyles? Styles { get; set; }

    /// <summary>
    /// The tooltip value of the flag element.
    /// </summary>
    /// <remarks>
    /// A tooltip is a pointer affordance: it is not an accessible name, and it never reaches a touch
    /// device at all. A flag that has to be named to everyone wants an <see cref="Alt"/> as well.
    /// <see cref="AutoTitle"/> fills this in with the country's own name.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The width of the flag, as any CSS length.
    /// </summary>
    /// <remarks>
    /// The flag images are square, so setting only a <see cref="Height"/> is usually enough - it sets
    /// the width as well. Both of them win over <see cref="Size"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    /// <summary>
    /// Gives focus to the flag element.
    /// </summary>
    /// <remarks>
    /// Only a flag the browser can focus takes it: one with an <see cref="OnClick"/> handler, or one
    /// given a <see cref="BitComponentBase.TabIndex"/> of its own. A decorative flag is not a tab stop
    /// and this does nothing to it.
    /// </remarks>
    /// <returns>
    /// A ValueTask that represents the asynchronous focus operation.
    /// </returns>
    public ValueTask FocusAsync() => RootElement.FocusAsync();

    /// <summary>
    /// Gives focus to the flag element, optionally without scrolling it into view.
    /// </summary>
    /// <param name="preventScroll">
    /// True to leave the page scrolled where it is instead of bringing the flag into view.
    /// </param>
    /// <returns>
    /// A ValueTask that represents the asynchronous focus operation.
    /// </returns>
    public ValueTask FocusAsync(bool preventScroll) => RootElement.FocusAsync(preventScroll);



    protected override string RootElementClass => "bit-flg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-flg-sm",
            BitSize.Medium => "bit-flg-md",
            BitSize.Large => "bit-flg-lg",
            _ => string.Empty
        });

        // The emoji is a glyph rather than a picture that fills the frame: it is as wide as the font
        // draws it, so the frame lets it out instead of clipping it to a square. Only a frame that was
        // not asked for a shape of its own, though - a circle drawn around a box grown to fit a glyph
        // is not a circle - so a shaped one keeps its size and crops the glyph the way it crops a
        // picture.
        ClassBuilder.Register(() => _emoji is not null && _shaped is false ? "bit-flg-emo" : string.Empty);

        // A frame given a ratio takes its width from that ratio and its height, so the square width
        // every other flag is given has to come off first.
        ClassBuilder.Register(() => AspectRatio.HasValue() ? "bit-flg-asp" : string.Empty);

        ClassBuilder.Register(() => Fit switch
        {
            BitImageFit.None => "bit-flg-non",
            BitImageFit.Center => "bit-flg-ctr",
            BitImageFit.CenterContain => "bit-flg-cct",
            BitImageFit.CenterCover => "bit-flg-ccv",
            BitImageFit.Contain => "bit-flg-cnt",
            BitImageFit.Cover => "bit-flg-cvr",
            BitImageFit.Fill => "bit-flg-fil",
            BitImageFit.ScaleDown => "bit-flg-scd",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Circular ? "bit-flg-cir" : (Rounded ? "bit-flg-rnd" : string.Empty));

        ClassBuilder.Register(() => Bordered ? "bit-flg-brd" : string.Empty);

        ClassBuilder.Register(() => Shadow ? "bit-flg-shd" : string.Empty);

        ClassBuilder.Register(() => Grayscale ? "bit-flg-gry" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && OnClick.HasDelegate ? "bit-flg-clk" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // One length is usually all a square flag has to be given, so a height alone sets the width as
        // well - and it is also what the emoji flag is drawn at, which is a font size rather than a
        // box, hence the custom property the stylesheet reads all three from.
        StyleBuilder.Register(() => (Height ?? Width).HasValue() ? $"--bit-flg-siz:{Height ?? Width}" : string.Empty);

        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);

        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);

        StyleBuilder.Register(() => AspectRatio.HasValue() ? $"aspect-ratio:{AspectRatio}" : string.Empty);

        // A ratio needs one of the two lengths left for it to work out. The width is the one the class
        // above frees, which is the usual way round; where the width is the length that was given, it
        // is the height that has to go.
        StyleBuilder.Register(() => AspectRatio.HasValue() && Width.HasValue() && Height.HasValue() is false
                                    ? "height:auto"
                                    : string.Empty);
    }

    protected override void OnParametersSet()
    {
        _country = ResolveCountry();

        // The emoji is built from the code rather than looked up, so a code the packaged images do not
        // cover still has an emoji flag - which is half of what makes this mode worth having.
        _emoji = Emoji ? BitCountries.GetEmoji(_country?.Iso2 ?? Iso2) : null;

        var iso2 = _country?.Iso2;
        var packaged = iso2.HasValue() ? GetFlagUrl(iso2!) : null;
        var src = Src.HasValue() ? Src : packaged;

        // A new source is a new image, so whatever the previous one ended up as is no longer the
        // answer: an error is forgotten and both images are drawn again. The packaged one is watched
        // as well as the one asked for, since a flag whose Src stayed put while its country moved on
        // has a different second image to try.
        if (string.Equals(src, _srcCandidate, StringComparison.Ordinal) is false ||
            string.Equals(packaged, _packagedSrc, StringComparison.Ordinal) is false)
        {
            _srcCandidate = src;
            _packagedSrc = packaged;
            _hasError = false;
            _hasFallbackError = false;
        }

        // A flag that has stopped answering - disabled, or handed a handler no longer there - has
        // nothing left to activate, so a Space it was already holding is forgotten rather than left
        // latched for the release that comes after.
        if (IsEnabled is false || OnClick.HasDelegate is false)
        {
            _spacePressed = false;
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Space scrolls the page by default, and a flag that answers Space is a control the reader
        // pressed it on rather than a place to scroll from. A Blazor keydown handler cannot decide that
        // per key - @onkeydown:preventDefault is evaluated at render time, so a flag set from the
        // handler lags a keystroke behind and would swallow the Tab that follows - so it is stopped on
        // a listener of the browser's own, registered for a flag that answers a click and emptied again
        // for one that has stopped answering, since the listener stays on the element and reads the key
        // list on every event. A disabled flag is one of those: it activates on nothing, so a Space
        // pressed on it - it can still be focused by name - scrolls the page the way it does anywhere
        // else.
        var interactive = IsEnabled && OnClick.HasDelegate;

        if (interactive == _preventKeysRegistered) return;

        try
        {
            await _js.BitUtilsRegisterPreventKeys(RootElement, interactive ? [" ", "Spacebar"] : []);

            // Only a call that landed counts as registered: flagging it beforehand would leave a failed
            // one believing the listener is installed, and the guard above would never let it be tried
            // again.
            _preventKeysRegistered = interactive;
        }
        catch (JSDisconnectedException) { } // the circuit is gone, nothing to register
        catch (JSException) { } // a JS-side failure here only costs the page-scroll prevention
    }



    /// <summary>
    /// The country the five ways of naming one resolve to, in the order they win in.
    /// </summary>
    /// <remarks>
    /// A <see cref="Country"/> is taken exactly as it was given rather than looked up: it is already
    /// the answer, and one of a page's own making is as valid as one out of the table. Everything else
    /// is resolved through <see cref="BitCountries"/>, so a code no country carries resolves to
    /// nothing rather than to an image that is not there.
    /// </remarks>
    private BitCountry? ResolveCountry()
    {
        if (Country is not null) return Country;

        return BitCountries.FindByIso2(Iso2)
            ?? BitCountries.FindByIso3(Iso3)
            ?? BitCountries.FindByCode(Code)
            ?? BitCountries.FindByName(Name);
    }

    /// <summary>
    /// The url of the packaged flag image of an ISO 3166-1 alpha-2 code.
    /// </summary>
    /// <remarks>
    /// Where the flag images live and how they are named is written down once, here, so that a
    /// component rendering one without going through <see cref="BitFlag"/> - the country list of
    /// <see cref="BitPhoneInput"/> - cannot drift from it.
    /// </remarks>
    internal static string GetFlagUrl(string iso2) => $"{FlagsPath}{iso2.ToUpperInvariant()}{FlagsSuffix}";

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    // The Enter key of a button activates it on the way down, and the Space key on the way up. The
    // Space that activates on the way up is kept from scrolling the page on the way down by the
    // listener registered in OnAfterRenderAsync.
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        // Only the press this element saw arms the release that follows: a Space released here after
        // being pressed elsewhere - the focus moved in while the key was already down - is not an
        // activation of it.
        if (IsSpace(e))
        {
            _spacePressed = true;

            return;
        }

        if (e.Key != "Enter") return;

        await OnClick.InvokeAsync(ToActivationArgs(e));
    }

    private async Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (IsSpace(e) is false) return;

        if (_spacePressed is false) return;

        _spacePressed = false;

        await OnClick.InvokeAsync(ToActivationArgs(e));
    }

    // A Space pressed here and released elsewhere - the focus moved on while the key was still down -
    // is not an activation of anything, so the press is forgotten as soon as the flag stops being the
    // element being pressed.
    private void HandleOnBlur() => _spacePressed = false;

    // An image that failed is not asked for again: left pointed at, the browser would fetch it once
    // per render. The packaged flag of the same country stands in for it where there is one to try,
    // and the fallback template where there is not.
    private async Task HandleOnError()
    {
        if (_hasError)
        {
            _hasFallbackError = true;
        }
        else
        {
            _hasError = true;
        }

        await OnError.InvokeAsync();
    }

    private Task HandleOnLoad() => OnLoad.InvokeAsync();

    private static bool IsSpace(KeyboardEventArgs e) => e.Key is " " or "Spacebar" || e.Code is "Space";

    // A click that came from a key carries no pointer: a detail of 0 is how the platform itself marks
    // one, and the modifiers are the only thing the keystroke has to pass on.
    private static MouseEventArgs ToActivationArgs(KeyboardEventArgs e) => new()
    {
        Detail = 0,
        AltKey = e.AltKey,
        CtrlKey = e.CtrlKey,
        MetaKey = e.MetaKey,
        ShiftKey = e.ShiftKey
    };
}
