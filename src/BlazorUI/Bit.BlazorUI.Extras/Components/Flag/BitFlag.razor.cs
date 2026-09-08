namespace Bit.BlazorUI;

/// <summary>
/// BitFlag is a component that renders the flag image of a country.
/// </summary>
/// <remarks>
/// The country is named in whichever of its five ways the page already has it in - a
/// <see cref="Country"/> out of <see cref="BitCountries"/>, an <see cref="Iso2"/> or <see cref="Iso3"/>
/// code, a dialing <see cref="Code"/> or an English <see cref="Name"/> - and the first of them that
/// resolves picks the flag. A country the table does not carry draws nothing at all rather than a
/// broken image, which is what <see cref="FallbackTemplate"/> is for.
/// <br />
/// What is drawn is a 16 pixel image out of the Extras package, unless <see cref="Emoji"/> asks for the
/// Unicode emoji flag instead - which is text, so it costs no request and stays crisp at any size,
/// where the image is only as sharp as its 16 pixels - or <see cref="Src"/> points the flag at a set of
/// images of the page's own. The frame around it takes the <see cref="Size"/>, or a
/// <see cref="Width"/> and <see cref="Height"/> of its own, and the shape: <see cref="Rounded"/>,
/// <see cref="Circular"/>, <see cref="Bordered"/> - which is what keeps a mostly white flag off a white
/// surface - <see cref="Shadow"/> and <see cref="Grayscale"/>.
/// <br />
/// To assistive technologies a flag is decorative by default: it nearly always sits beside the name of
/// the very country it stands for, and a second reading of that name is noise. A flag that carries
/// meaning of its own is named with an <see cref="Alt"/>, or with the country's own name through
/// <see cref="AutoAlt"/>, and <see cref="AutoTitle"/> does the same for the tooltip. An
/// <see cref="OnClick"/> turns the whole frame into a button that answers the keyboard as well as the
/// pointer, which is what a flag standing for a language or a locale in a picker needs.
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
    /// Whether the image currently pointed at has already failed to load. An image that failed is not
    /// drawn again - the browser would ask for it once per render - and the fallback stands in its
    /// place instead.
    /// </summary>
    private bool _hasError;

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

    private string? _src => _hasError ? null : _srcCandidate;

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
    /// cell, or as the whole of a language switcher - rather than beside the name it would repeat. An
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
    /// load, which is the <see cref="Src"/> of a page pointing at a set of its own. Without one, such a
    /// flag draws nothing and leaves an empty frame of its own size.
    /// </remarks>
    [Parameter] public RenderFragment? FallbackTemplate { get; set; }

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
    /// The ISO 3166-1 alpha-2 code of the country.
    /// </summary>
    /// <remarks>
    /// It is matched case insensitively against <see cref="BitCountries.All"/>, and a code no country
    /// of the table carries draws nothing rather than asking the network for an image that is not
    /// there.
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
    /// <see cref="BitCountries.All"/>, rather than against part of it.
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
    /// on its own.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

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
    /// flag and fills in its tooltip; a source that fails to load falls back to
    /// <see cref="FallbackTemplate"/>. <see cref="Emoji"/> wins over it.
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
        // draws it, so the frame lets it out instead of clipping it to a square.
        ClassBuilder.Register(() => _emoji is not null ? "bit-flg-emo" : string.Empty);

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
    }

    protected override void OnParametersSet()
    {
        _country = ResolveCountry();

        // The emoji is built from the code rather than looked up, so a code the packaged images do not
        // cover still has an emoji flag - which is half of what makes this mode worth having.
        _emoji = Emoji ? BitCountries.GetEmoji(_country?.Iso2 ?? Iso2) : null;

        var src = ResolveSrc();

        // A new source is a new image, so whatever the previous one ended up as is no longer the
        // answer: an error is forgotten and the image is drawn again.
        if (string.Equals(src, _srcCandidate, StringComparison.Ordinal) is false)
        {
            _srcCandidate = src;
            _hasError = false;
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
        // a listener of the browser's own, registered for a flag that has a click handler and emptied
        // again for one that has lost it, since the listener stays on the element and reads the key
        // list on every event.
        var interactive = OnClick.HasDelegate;

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

    private string? ResolveSrc()
    {
        if (Src.HasValue()) return Src;

        var iso2 = _country?.Iso2;

        return iso2.HasValue() ? GetFlagUrl(iso2!) : null;
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
    // per render. The fallback stands in its place instead.
    private void HandleOnError() => _hasError = true;

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
