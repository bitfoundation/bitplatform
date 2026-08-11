namespace Bit.BlazorUI;

/// <summary>
/// Carousel (slide-show) let people show their items in separate slides from two or more items.
/// </summary>
/// <remarks>
/// The slides are laid out side by side inside a clipping box and moved with a transform, so a page of
/// <see cref="VisibleItemsCount"/> items slides in while the previous page slides out. The carousel can be
/// driven with its own next/prev buttons, with the dots below it, by dragging it, with the keyboard, with the
/// mouse wheel, from application code through <see cref="GoNext"/>/<see cref="GoPrev"/>/<see cref="GoTo(int)"/>,
/// or on its own with <see cref="AutoPlay"/>.
/// <br />
/// It follows the carousel pattern of the ARIA authoring practices: the root is a labelled region marked as a
/// carousel, every slide is a labelled group, the controls are real buttons, and automatic rotation stops as
/// soon as the pointer or the keyboard focus enters the carousel so it can never move content out from under
/// the person reading it.
/// </remarks>
public partial class BitCarousel : BitComponentBase
{
    private int _pagesCount;
    private int _currentPage;
    private double _pointerX;
    private double _pointerY;
    private bool _hovered;
    private bool _stopped;
    private bool _focused;
    private bool _isPaused;
    private bool _navigating;
    private bool _pageHidden;
    private bool _needsReset;
    private bool _isPointerDown;
    private bool _afterFirstRender;
    private string _layoutSignature = string.Empty;
    private int[] _othersIndices = [];
    private int[] _currentIndices = [];
    private DateTime _lastWheel = DateTime.MinValue;
    private int _internalScrollItemsCount = 1;
    private int _internalVisibleItemsCount = 1;
    private System.Timers.Timer? _autoPlayTimer;
    private string _directionStyle = string.Empty;
    private string _goLeftButtonStyle = string.Empty;
    private string _goRightButtonStyle = string.Empty;
    private readonly List<BitCarouselItem> _allItems = [];
    private ElementReference _carouselContainer = default!;
    private DotNetObjectReference<BitCarousel> _dotnetObj = default!;

    // The keys the carousel acts on are also the keys the browser scrolls the page with, so they are
    // swallowed on the way in. They are handled in the browser rather than with Blazor's preventDefault
    // directive, because the directive is static while the carousel only owns these keys while the
    // keyboard is enabled. Only the keys of the axis the carousel actually scrolls on are taken, so a
    // horizontal carousel does not keep the page from being scrolled with the up/down arrow keys.
    private static readonly string[] _horizontalNavigationKeys = ["ArrowLeft", "ArrowRight", "Home", "End"];
    private static readonly string[] _verticalNavigationKeys = ["ArrowUp", "ArrowDown", "Home", "End"];



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private BitPageVisibility _pageVisibility { get; set; } = default!;



    /// <summary>
    /// Specifies the accent color kind of the component.
    /// </summary>
    /// <remarks>
    /// It colors the dot of the current page. <see cref="Color"/> takes precedence over it when both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Accent { get; set; }

    /// <summary>
    /// Sets the duration of the scrolling animation in seconds (the default value is 0.5).
    /// </summary>
    /// <remarks>
    /// It is the duration of the slide of a page in and out of the view, and of the cross-fade of the
    /// <see cref="Fade"/> effect. A value of 0 moves the slides at once, with no animation at all.
    /// </remarks>
    [Parameter] public double AnimationDuration { get; set; } = 0.5;

    /// <summary>
    /// Enables/disables the auto scrolling of the slides.
    /// </summary>
    /// <remarks>
    /// The rotation pauses on its own while the pointer is over the carousel (<see cref="PauseOnHover"/>),
    /// while the keyboard focus is inside it (<see cref="PauseOnFocus"/>) and while the page is hidden in a
    /// background tab, and it can be stopped from application code with <see cref="Pause"/> or by the person
    /// reading the page with the button <see cref="ShowPlayPause"/> renders.
    /// <br />
    /// A carousel that reaches its last page rewinds to the first one, unless <see cref="StopOnLastSlide"/>
    /// says to stop there.
    /// </remarks>
    [Parameter] public bool AutoPlay { get; set; }

    /// <summary>
    /// Sets the interval of the auto scrolling in milliseconds (the default value is 2000).
    /// </summary>
    [Parameter] public double AutoPlayInterval { get; set; } = 2000;

    /// <summary>
    /// Plays the auto scrolling backwards, from the last slide towards the first one.
    /// </summary>
    [Parameter] public bool AutoPlayReverse { get; set; }

    /// <summary>
    /// Items of the carousel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom CSS classes for the different parts of the carousel.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitCarouselClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the carousel.
    /// </summary>
    /// <remarks>
    /// It colors the dot of the current page and the next/prev and play/pause buttons.
    /// <br />
    /// When not set, the carousel falls back to <see cref="Accent"/>, and to the primary color of the theme
    /// when that is not set either.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The page (1 based, like <see cref="GoTo(int)"/>) the carousel shows when it first renders.
    /// </summary>
    /// <remarks>
    /// It is only read while the carousel is being laid out for the first time, so changing it afterwards
    /// does not move the carousel. Use <see cref="GoTo(int)"/> for that. Values outside of the range of
    /// the carousel are clamped to its first or last page.
    /// </remarks>
    [Parameter] public int DefaultPage { get; set; } = 1;

    /// <summary>
    /// The accessible label of a dot of the carousel, followed by the number of the page it navigates to.
    /// </summary>
    [Parameter] public string DotAriaLabel { get; set; } = "Slide";

    /// <summary>
    /// The accessible label of the dots container of the carousel.
    /// </summary>
    [Parameter] public string DotsAriaLabel { get; set; } = "Choose slide to display";

    /// <summary>
    /// The distance (in pixels) the pointer has to travel over the carousel before it moves to another page.
    /// </summary>
    /// <remarks>
    /// A drag that travels further across the other axis than along the one the carousel scrolls is left
    /// alone, so scrolling a page by dragging over a carousel does not flip its slides.
    /// </remarks>
    [Parameter] public int DragThreshold { get; set; } = 20;

    /// <summary>
    /// Cross-fades the slides in place instead of sliding them.
    /// </summary>
    /// <remarks>
    /// A fading carousel shows exactly one slide at a time, so <see cref="VisibleItemsCount"/> and
    /// <see cref="ScrollItemsCount"/> are both taken as 1 while it is enabled.
    /// </remarks>
    [Parameter] public bool Fade { get; set; }

    /// <summary>
    /// The space between the slides of the carousel (any CSS length, for example <c>1rem</c>).
    /// </summary>
    /// <remarks>
    /// The gap is taken out of the room each slide already occupies, so adding one never changes how many
    /// slides fit in the carousel.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// The accessible label of the go to left button of the carousel.
    /// </summary>
    /// <remarks>
    /// When not set, the button is labelled after what it actually does, which is the next slide in a
    /// left-to-right carousel and the previous one in a right-to-left carousel.
    /// </remarks>
    [Parameter] public string? GoLeftAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon for the go to left button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="GoLeftIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="GoLeftIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: GoLeftIcon="BitIconInfo.Bi("chevron-left")"
    /// FontAwesome: GoLeftIcon="BitIconInfo.Fa("solid chevron-left")"
    /// Custom CSS: GoLeftIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? GoLeftIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon for the go to left button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChevronLeft</c>).
    /// <br />
    /// For external icon libraries, use <see cref="GoLeftIcon"/> instead.
    /// </remarks>
    [Parameter] public string? GoLeftIconName { get; set; }

    /// <summary>
    /// The accessible label of the go to right button of the carousel.
    /// </summary>
    /// <remarks>
    /// When not set, the button is labelled after what it actually does, which is the previous slide in a
    /// left-to-right carousel and the next one in a right-to-left carousel.
    /// </remarks>
    [Parameter] public string? GoRightAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon for the go to right button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="GoRightIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="GoRightIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: GoRightIcon="BitIconInfo.Bi("chevron-right")"
    /// FontAwesome: GoRightIcon="BitIconInfo.Fa("solid chevron-right")"
    /// Custom CSS: GoRightIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? GoRightIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon for the go to right button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChevronRight</c>).
    /// <br />
    /// For external icon libraries, use <see cref="GoRightIcon"/> instead.
    /// </remarks>
    [Parameter] public string? GoRightIconName { get; set; }

    /// <summary>
    /// Hides the Dots indicator at the bottom of the BitCarousel.
    /// </summary>
    /// <remarks>
    /// The dots are also left out when everything fits on a single page, since there is nothing to
    /// navigate to then.
    /// </remarks>
    [Parameter] public bool HideDots { get; set; }

    /// <summary>
    /// Hides the Next/Prev buttons of the BitCarousel.
    /// </summary>
    /// <remarks>
    /// Each button also hides itself at the end it cannot move any further towards, unless
    /// <see cref="InfiniteScrolling"/> is enabled.
    /// </remarks>
    [Parameter] public bool HideNextPrev { get; set; }

    /// <summary>
    /// If enabled the carousel items will navigate in an infinite loop (first item comes after last item and last item comes before first item).
    /// </summary>
    [Parameter] public bool InfiniteScrolling { get; set; }

    /// <summary>
    /// Disables dragging the carousel with the pointer.
    /// </summary>
    [Parameter] public bool NoDrag { get; set; }

    /// <summary>
    /// Removes the carousel from the tab sequence and turns off its keyboard navigation.
    /// </summary>
    /// <remarks>
    /// A carousel is normally focusable so it can be operated with the arrow keys (and with Home and End),
    /// which is the only way to reach a slide for someone who cannot use a pointer. Only turn this off when
    /// the same navigation is offered somewhere else on the page.
    /// </remarks>
    [Parameter] public bool NoKeyboard { get; set; }

    /// <summary>
    /// The event that will be called on carousel page navigation.
    /// </summary>
    /// <remarks>
    /// The provided value is the zero based index of the page the carousel moved to.
    /// </remarks>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// The accessible label of the play/pause button while the auto scrolling is running.
    /// </summary>
    [Parameter] public string PauseButtonAriaLabel { get; set; } = "Stop automatic slide show";

    /// <summary>
    /// Gets or sets the icon of the play/pause button while the auto scrolling is running, using custom CSS
    /// classes for external icon libraries. Takes precedence over <see cref="PauseIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PauseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the play/pause button while the auto scrolling is running, from
    /// the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? PauseIconName { get; set; }

    /// <summary>
    /// Pauses the auto scrolling while the keyboard focus is inside the carousel (the default value is true).
    /// </summary>
    /// <remarks>
    /// Turning this off makes the carousel move the control that is being operated with the keyboard out of
    /// the view, so leave it on unless the slides hold nothing focusable at all.
    /// </remarks>
    [Parameter] public bool PauseOnFocus { get; set; } = true;

    /// <summary>
    /// Pauses the auto scrolling while the pointer is over the carousel (the default value is true).
    /// </summary>
    [Parameter] public bool PauseOnHover { get; set; } = true;

    /// <summary>
    /// The accessible label of the play/pause button while the auto scrolling is paused.
    /// </summary>
    [Parameter] public string PlayButtonAriaLabel { get; set; } = "Start automatic slide show";

    /// <summary>
    /// Gets or sets the icon of the play/pause button while the auto scrolling is paused, using custom CSS
    /// classes for external icon libraries. Takes precedence over <see cref="PlayIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PlayIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the play/pause button while the auto scrolling is paused, from
    /// the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? PlayIconName { get; set; }

    /// <summary>
    /// Number of items that is going to be changed on navigation.
    /// </summary>
    /// <remarks>
    /// It is clamped to <see cref="VisibleItemsCount"/>, since a carousel cannot skip over slides that were
    /// never shown. A non-infinite carousel also moves by fewer items near its ends when a full move would
    /// overshoot them, so its first and last pages always stay full.
    /// </remarks>
    [Parameter] public int ScrollItemsCount { get; set; } = 1;

    /// <summary>
    /// Renders a play/pause button next to the dots, so the auto scrolling can be stopped and started again.
    /// </summary>
    /// <remarks>
    /// Only rendered while <see cref="AutoPlay"/> is enabled. Offering this control is what the "pause,
    /// stop, hide" success criterion of WCAG asks of any content that moves on its own for more than five
    /// seconds, so turn it on for a carousel that carries meaningful content.
    /// </remarks>
    [Parameter] public bool ShowPlayPause { get; set; }

    /// <summary>
    /// The size of the dots and of the next/prev buttons of the carousel.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Stops the auto scrolling as soon as the carousel is navigated by hand.
    /// </summary>
    /// <remarks>
    /// Once stopped this way the rotation only comes back through <see cref="Resume"/> or the play/pause
    /// button, which is what someone who took over the carousel expects.
    /// </remarks>
    [Parameter] public bool StopOnInteraction { get; set; }

    /// <summary>
    /// Stops the auto scrolling on the last page instead of rewinding to the first one.
    /// </summary>
    /// <remarks>
    /// It has no effect while <see cref="InfiniteScrolling"/> is enabled, where there is no last page to
    /// stop on.
    /// </remarks>
    [Parameter] public bool StopOnLastSlide { get; set; }

    /// <summary>
    /// The custom CSS styles for the different parts of the carousel.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitCarouselClassStyles? Styles { get; set; }

    /// <summary>
    /// Stacks the slides vertically, so the carousel scrolls up and down instead of left and right.
    /// </summary>
    /// <remarks>
    /// A vertical carousel is laid out over the height of its root element, so give it one (the default is
    /// a fixed height, and any height set through <see cref="BitComponentBase.Style"/> or a class replaces
    /// it). Its direction does not flip in right-to-left, since only the horizontal axis has a direction.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }

    /// <summary>
    /// Number of items that is visible in the carousel.
    /// </summary>
    [Parameter] public int VisibleItemsCount { get; set; } = 1;

    /// <summary>
    /// Navigates the carousel with the wheel of the mouse (or with a two finger scroll on a trackpad).
    /// </summary>
    /// <remarks>
    /// The carousel keeps the wheel to itself while this is enabled, so the page underneath it does not
    /// scroll while the pointer is over the carousel.
    /// </remarks>
    [Parameter] public bool Wheel { get; set; }



    /// <summary>
    /// The zero based index of the page the carousel is currently showing.
    /// </summary>
    public int CurrentPage => _currentPage;

    /// <summary>
    /// The number of items (slides) of the carousel.
    /// </summary>
    public int ItemsCount => _allItems.Count;

    /// <summary>
    /// Whether the auto scrolling is currently running.
    /// </summary>
    /// <remarks>
    /// It is false while the rotation is paused for any reason, including the pointer resting over the
    /// carousel and the page sitting in a background tab.
    /// </remarks>
    public bool IsPlaying => _autoPlayTimer?.Enabled ?? false;

    /// <summary>
    /// Whether the auto scrolling has been paused through <see cref="Pause"/> or the play/pause button.
    /// </summary>
    public bool IsPaused => _isPaused || _stopped;

    /// <summary>
    /// The number of pages of the carousel.
    /// </summary>
    public int PagesCount => _pagesCount;



    /// <summary>
    /// Navigates to the next carousel item.
    /// </summary>
    public async Task GoNext()
    {
        await Next();
    }

    /// <summary>
    /// Navigates to the previous carousel item.
    /// </summary>
    public async Task GoPrev()
    {
        await Prev();
    }

    /// <summary>
    /// Navigates to the given carousel page number (1 based).
    /// </summary>
    /// <param name="number">
    /// The 1 based number of the page to navigate to. Values outside of the range of the carousel are
    /// clamped to its first or last page, or wrapped around while <see cref="InfiniteScrolling"/> is enabled.
    /// </param>
    public async Task GoTo(int number)
    {
        await GotoPage(number - 1);
    }

    /// <summary>
    /// Pauses the AutoPlay if enabled.
    /// </summary>
    public void Pause()
    {
        _isPaused = true;

        UpdateAutoPlayTimer();
    }

    /// <summary>
    /// Resumes the AutoPlay if enabled.
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        _stopped = false;

        UpdateAutoPlayTimer();
    }

    /// <summary>
    /// Pauses the AutoPlay when it is running, and resumes it when it is paused.
    /// </summary>
    public void TogglePlay()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// Measures the carousel again and lays its slides out accordingly.
    /// </summary>
    /// <remarks>
    /// The carousel already does this on its own whenever it is resized and whenever its items change, so
    /// this is only needed after something else moved the ground under it (a container that changed size
    /// without the carousel changing size with it, for example).
    /// </remarks>
    public async Task Refresh()
    {
        await ResetDimensionsAsync();
    }



    [JSInvokable("OnResize")]
    public async Task _OnResize(ContentRect rect)
    {
        if (IsDisposed) return;

        await ResetDimensionsAsync();
    }



    internal void RegisterItem(BitCarouselItem item)
    {
        item.Index = _allItems.Count;

        _allItems.Add(item);

        _needsReset = true;

        StateHasChanged();
    }

    internal void UnregisterItem(BitCarouselItem carouselItem)
    {
        if (_allItems.Remove(carouselItem) is false) return;

        // The indices of the items are what every calculation of the carousel is written in terms of, so
        // the ones after the removed item are moved up rather than left pointing one slot too far.
        for (int i = 0; i < _allItems.Count; i++)
        {
            _allItems[i].Index = i;
        }

        if (IsDisposed) return;

        _needsReset = true;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-csl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Vertical ? "bit-csl-vrt" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-csl-sm",
            BitSize.Medium => "bit-csl-md",
            BitSize.Large => "bit-csl-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Accent switch
        {
            BitColorKind.Primary => "bit-csl-apri",
            BitColorKind.Secondary => "bit-csl-asec",
            BitColorKind.Tertiary => "bit-csl-ater",
            BitColorKind.Transparent => "bit-csl-atra",
            _ => "bit-csl-apri"
        });

        // The color classes come after the accent ones in the stylesheet, so a carousel that sets both
        // ends up with the color it was given rather than with the accent it fell back to.
        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-csl-pri",
            BitColor.Secondary => "bit-csl-sec",
            BitColor.Tertiary => "bit-csl-ter",
            BitColor.Info => "bit-csl-inf",
            BitColor.Success => "bit-csl-suc",
            BitColor.Warning => "bit-csl-wrn",
            BitColor.SevereWarning => "bit-csl-swr",
            BitColor.Error => "bit-csl-err",
            BitColor.PrimaryBackground => "bit-csl-pbg",
            BitColor.SecondaryBackground => "bit-csl-sbg",
            BitColor.TertiaryBackground => "bit-csl-tbg",
            BitColor.PrimaryForeground => "bit-csl-pfg",
            BitColor.SecondaryForeground => "bit-csl-sfg",
            BitColor.TertiaryForeground => "bit-csl-tfg",
            BitColor.PrimaryBorder => "bit-csl-pbr",
            BitColor.SecondaryBorder => "bit-csl-sbr",
            BitColor.TertiaryBorder => "bit-csl-tbr",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Gap.HasValue() ? $"--bit-csl-gap:{Gap}" : string.Empty);
    }

    protected override void OnInitialized()
    {
        _pageVisibility.OnChange += PageVisibilityChange;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        _directionStyle = Dir == BitDir.Rtl ? "direction:rtl" : string.Empty;

        ClampCounts();

        // Everything that changes where the slides sit is folded into one signature, so a single
        // comparison decides whether the carousel has to be measured and laid out again.
        var signature = FormattableString.Invariant(
            $"{_internalVisibleItemsCount}|{_internalScrollItemsCount}|{Vertical}|{Fade}|{InfiniteScrolling}|{Dir}|{NoKeyboard}|{NoDrag}|{IsEnabled}");

        if (_layoutSignature != signature)
        {
            _layoutSignature = signature;

            _needsReset = _afterFirstRender;
        }

        UpdateAutoPlayTimer();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            await _js.BitObserversRegisterResize(UniqueId, RootElement, _dotnetObj);

            _afterFirstRender = true;
            _needsReset = false;

            // The page the carousel starts on is applied before the first measurement, so it is laid
            // out on that page instead of sliding over to it after the fact. The counts are clamped
            // here because the items were not registered yet when the parameters were set.
            ClampCounts();

            var visible = Math.Max(1, _internalVisibleItemsCount);
            var pages = (int)Math.Ceiling((decimal)_allItems.Count / visible);
            var startPage = Math.Clamp(DefaultPage - 1, 0, Math.Max(0, pages - 1));

            _currentIndices = [startPage * visible];

            await ResetDimensionsAsync();

            await RegisterPreventDefaultsAsync();

            await _pageVisibility.Init();

            UpdateAutoPlayTimer();
        }
        else if (_needsReset)
        {
            _needsReset = false;

            await ResetDimensionsAsync();

            await RegisterPreventDefaultsAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private async Task RegisterPreventDefaultsAsync()
    {
        var keys = (NoKeyboard || IsEnabled is false) ? [] : (Vertical ? _verticalNavigationKeys : _horizontalNavigationKeys);

        await _js.BitUtilsRegisterPreventKeys(RootElement, keys);

        // Dragging a slide must not start the browser's own drag of an image inside it, but it is
        // suppressed in the browser rather than with Blazor's static preventDefault directive, so a
        // pointerdown on the next/prev buttons is left alone and they can still take the focus.
        await _js.BitUtilsRegisterPreventPointerDown(_carouselContainer, NoDrag is false && IsEnabled);
    }

    // Both counts have to make sense on their own (a carousel showing zero items would divide by zero) and
    // against each other (a carousel cannot scroll past what it shows, or show more slides than it holds).
    private void ClampCounts()
    {
        var itemsCount = _allItems.Count;

        var visible = Fade ? 1 : Math.Max(1, VisibleItemsCount);

        if (itemsCount > 0 && visible > itemsCount)
        {
            visible = itemsCount;
        }

        _internalVisibleItemsCount = visible;

        _internalScrollItemsCount = Fade ? 1 : Math.Clamp(ScrollItemsCount, 1, visible);
    }

    private string Translate(double percent)
    {
        return Vertical
            ? FormattableString.Invariant($"transform:translateY({percent}%)")
            : FormattableString.Invariant($"transform:translateX({percent}%)");
    }

    private string Transition()
    {
        var duration = Math.Max(0, AnimationDuration);

        return FormattableString.Invariant($"transition:all {duration}s");
    }

    private async Task ResetDimensionsAsync()
    {
        if (IsDisposed) return;

        ClampCounts();

        var itemsCount = _allItems.Count;
        var visible = _internalVisibleItemsCount;

        // The page the carousel is on is kept across a re-layout, so resizing the window (or adding a slide)
        // does not throw the reader back to the first page.
        var first = _currentIndices.Length > 0 ? _currentIndices[0] : 0;

        if (first < 0 || first >= itemsCount) first = 0;

        if (InfiniteScrolling is false)
        {
            first = Math.Min(first, Math.Max(0, itemsCount - visible));
        }

        // A page of an infinite carousel can borrow slides from the other end (GotoPage wraps the
        // indices that run past the last item around to the first one), so a re-layout wraps them
        // the same way instead of cutting the borrowed slides off.
        _currentIndices = InfiniteScrolling && itemsCount > 0
            ? Enumerable.Range(first, visible).Select(i => i % itemsCount).ToArray()
            : Enumerable.Range(first, visible).Where(i => i < itemsCount).ToArray();
        _othersIndices = [];

        _pagesCount = visible > 0 ? (int)Math.Ceiling((decimal)itemsCount / visible) : 0;
        _currentPage = CalculateCurrentPage(_currentIndices, visible);

        SetNavigationButtonsVisibility();
        UpdateItemsCurrentState();

        // The page count above is what the auto play hinges on, so the timer is re-evaluated here:
        // a carousel that only just received enough items to have somewhere to rotate to starts, and
        // one whose items were taken away stops.
        UpdateAutoPlayTimer();

        // The transform of a slide and the opacity of the fade effect belong to a layout, so they
        // are cleared up front rather than left behind stale when the measurement below fails.
        foreach (var item in _allItems)
        {
            item.InternalFadeStyle = string.Empty;
            item.InternalTransformStyle = string.Empty;
        }

        var rect = await _js.BitUtilsGetBoundingClientRect(_carouselContainer);

        if (IsDisposed) return;

        if (rect is not null)
        {
            var size = Vertical ? rect.Height : rect.Width;
            var itemSize = visible > 0 ? size / visible : size;
            var sign = (Vertical is false && Dir == BitDir.Rtl) ? -1 : 1;

            for (int i = 0; i < itemsCount; i++)
            {
                var item = _allItems[i];

                item.InternalStyle = Vertical
                    ? FormattableString.Invariant($"height:{itemSize}px;width:100%;display:block")
                    : FormattableString.Invariant($"width:{itemSize}px;display:block");

                item.InternalTransitionStyle = string.Empty;

                if (Fade)
                {
                    var isCurrent = Array.IndexOf(_currentIndices, i) >= 0;

                    item.InternalTransformStyle = string.Empty;
                    item.InternalFadeStyle = isCurrent ? "opacity:1;z-index:1" : "opacity:0;z-index:0";
                }
                else
                {
                    // A slide that is on screen sits at its place in the visible run, and everything else
                    // is laid out relative to the first visible slide, which is what puts the slides before
                    // the current page out to its start rather than piling them up after it.
                    var position = Array.IndexOf(_currentIndices, i);

                    if (position < 0) position = i - first;

                    item.InternalFadeStyle = string.Empty;
                    item.InternalTransformStyle = Translate(sign * 100 * position);
                }
            }
        }

        StateHasChanged();
    }

    private void UpdateItemsCurrentState()
    {
        for (int i = 0; i < _allItems.Count; i++)
        {
            _allItems[i].InternalIsCurrent = Array.IndexOf(_currentIndices, i) >= 0;
        }
    }

    private void SetNavigationButtonsVisibility()
    {
        var itemsCount = _allItems.Count;

        var atStart = InfiniteScrolling is false && (_currentIndices.Length == 0 || _currentIndices[0] <= 0);
        var atEnd = InfiniteScrolling is false && (_currentIndices.Length == 0 || _currentIndices[^1] >= itemsCount - 1);

        // A carousel with a single page has nowhere to go, so neither button is of any use on it.
        var single = _pagesCount < 2;

        // The button on the left of a right-to-left carousel is the one that goes back, so which end
        // silences which button flips with the direction. The vertical axis has no direction to flip.
        var isRtl = Vertical is false && Dir == BitDir.Rtl;

        _goLeftButtonStyle = (single || (isRtl ? atStart : atEnd)) ? "display:none" : string.Empty;
        _goRightButtonStyle = (single || (isRtl ? atEnd : atStart)) ? "display:none" : string.Empty;
    }

    private bool CanGoNext()
    {
        if (_allItems.Count == 0) return false;
        if (InfiniteScrolling) return true;

        return _currentIndices.Length > 0 && _currentIndices[^1] < _allItems.Count - 1;
    }

    private bool CanGoPrev()
    {
        if (_allItems.Count == 0) return false;
        if (InfiniteScrolling) return true;

        return _currentIndices.Length > 0 && _currentIndices[0] > 0;
    }

    private async Task GoLeft() => await ((Vertical is false && Dir == BitDir.Rtl) ? Prev() : Next());

    private async Task GoRight() => await ((Vertical is false && Dir == BitDir.Rtl) ? Next() : Prev());

    // Everything the person reading the page does to the carousel by hand goes through here first, so the
    // auto scrolling can step aside for them when it was asked to.
    private void HandleInteraction()
    {
        if (StopOnInteraction is false) return;
        if (AutoPlay is false) return;

        _stopped = true;

        UpdateAutoPlayTimer();
    }

    private async Task HandleGoLeft()
    {
        HandleInteraction();

        await GoLeft();
    }

    private async Task HandleGoRight()
    {
        HandleInteraction();

        await GoRight();
    }

    private async Task HandleGotoPage(int index)
    {
        HandleInteraction();

        await GotoPage(index);
    }

    private async Task Prev()
    {
        if (IsEnabled is false) return;

        var itemsCount = _allItems.Count;

        if (itemsCount == 0 || _currentIndices.Length == 0) return;

        // Moving back by fewer items than asked for keeps the first page full, instead of leaving a
        // blank space before the first item when the items do not divide evenly into pages.
        var count = InfiniteScrolling
            ? _internalScrollItemsCount
            : Math.Min(_internalScrollItemsCount, _currentIndices[0]);

        if (count < 1) return;

        _othersIndices = Enumerable.Range(0, count).Select(i =>
        {
            var idx = _currentIndices[0] - (i + 1);
            if (InfiniteScrolling && idx < 0)
            {
                idx += itemsCount;
            }
            return idx;
        }).Where(i => i >= 0 && i < itemsCount).Reverse().ToArray();

        await Go(false, count);
    }

    private async Task Next()
    {
        if (IsEnabled is false) return;

        var itemsCount = _allItems.Count;

        if (itemsCount == 0 || _currentIndices.Length == 0) return;

        // Moving forward by fewer items than asked for keeps the last page full, instead of leaving a
        // blank space after the last item when the items do not divide evenly into pages.
        var count = InfiniteScrolling
            ? _internalScrollItemsCount
            : Math.Min(_internalScrollItemsCount, itemsCount - 1 - _currentIndices[^1]);

        if (count < 1) return;

        _othersIndices = Enumerable.Range(0, count).Select(i =>
        {
            var idx = _currentIndices[^1] + (i + 1);
            if (InfiniteScrolling && idx > itemsCount - 1)
            {
                idx -= itemsCount;
            }
            return idx;
        }).Where(i => i >= 0 && i < itemsCount).ToArray();

        await Go(true, count);
    }

    private async Task Go(bool isNext = false, int scrollCount = 0)
    {
        if (IsDisposed) return;
        if (IsEnabled is false) return;
        if (_othersIndices.Length == 0) return;

        // A second move started while the first one is still being handed to the browser would read the
        // half applied state as if it were settled, and leave slides behind at the wrong offsets.
        if (_navigating) return;

        var visible = _internalVisibleItemsCount;

        if (scrollCount < 1)
        {
            scrollCount = _internalScrollItemsCount;
        }

        scrollCount = Math.Clamp(scrollCount, 1, visible);

        var diff = visible - scrollCount;
        var newIndices = (isNext
            ? _currentIndices.Skip(scrollCount).Take(diff).Concat(_othersIndices)
            : _othersIndices.Concat(_currentIndices.Take(diff))).ToArray();

        var currents = _currentIndices.Where(IsValidIndex).Select(i => _allItems[i]).ToArray();
        var others = _othersIndices.Where(IsValidIndex).Select(i => _allItems[i]).ToArray();

        if (others.Length == 0) return;

        if (Fade)
        {
            var transition = FormattableString.Invariant($"transition:opacity {Math.Max(0, AnimationDuration)}s");

            foreach (var c in currents)
            {
                c.InternalTransitionStyle = transition;
                c.InternalFadeStyle = "opacity:0;z-index:0";
            }

            foreach (var o in others)
            {
                o.InternalTransitionStyle = transition;
                o.InternalFadeStyle = "opacity:1;z-index:1";
            }

            await ApplyNewIndices(newIndices);

            return;
        }

        var sign = isNext ? 1 : -1;
        var offset = isNext ? visible : scrollCount;

        // The incoming slides are put just outside the view without a transition first, so the move
        // below animates from the edge of the carousel instead of from wherever they happened to sit.
        for (int i = 0; i < others.Length; i++)
        {
            var o = others[i];
            o.InternalTransitionStyle = string.Empty;
            var x = sign * 100 * (offset + (sign * i));
            x = (Vertical is false && Dir == BitDir.Rtl) ? -x : x;
            o.InternalTransformStyle = Translate(x);
        }

        _navigating = true;

        // The flag covers the whole move, up to and including the bookkeeping of the new indices.
        try
        {
            StateHasChanged();

            // The placement above has to reach the browser as a frame of its own, otherwise it is coalesced
            // with the move below into a single style change and there is nothing left to animate.
            await Task.Delay(50);

            if (IsDisposed) return;

            offset = isNext ? visible - scrollCount : 0;

            var transitionStyle = Transition();

            for (int i = 0; i < currents.Length; i++)
            {
                var c = currents[i];
                c.InternalTransitionStyle = transitionStyle;
                var x = -sign * 100 * (scrollCount + (-sign * i));
                x = (Vertical is false && Dir == BitDir.Rtl) ? -x : x;
                c.InternalTransformStyle = Translate(x);
            }

            for (int i = 0; i < others.Length; i++)
            {
                var o = others[i];
                o.InternalTransitionStyle = transitionStyle;
                var x = 100 * (offset + i);
                x = (Vertical is false && Dir == BitDir.Rtl) ? -x : x;
                o.InternalTransformStyle = Translate(x);
            }

            await ApplyNewIndices(newIndices);
        }
        finally
        {
            _navigating = false;
        }
    }

    private bool IsValidIndex(int index) => index >= 0 && index < _allItems.Count;

    // The page is where the first visible item falls, except at the end of a non-infinite carousel: its
    // last page is aligned to the last item when the items do not divide evenly into pages, so standing
    // on the last item is what standing on the last page means there.
    private int CalculateCurrentPage(int[] indices, int visible)
    {
        if (visible < 1 || indices.Length == 0) return 0;

        if (InfiniteScrolling is false && _pagesCount > 0 && indices[^1] == _allItems.Count - 1)
        {
            return _pagesCount - 1;
        }

        return (int)Math.Floor((decimal)indices[0] / visible);
    }

    private async Task ApplyNewIndices(int[] newIndices)
    {
        var visible = _internalVisibleItemsCount;
        var previousPage = _currentPage;

        _currentIndices = newIndices;
        _currentPage = CalculateCurrentPage(newIndices, visible);

        UpdateItemsCurrentState();
        SetNavigationButtonsVisibility();

        // The interval is measured from the end of a move, so a slide that was navigated by hand is not
        // whisked away by the auto scrolling a moment later.
        RestartAutoPlayTimer();

        StateHasChanged();

        if (previousPage != _currentPage)
        {
            await OnChange.InvokeAsync(_currentPage);
        }
    }

    private async Task GotoPage(int index)
    {
        if (IsEnabled is false) return;
        if (_pagesCount < 1) return;
        if (_currentIndices.Length == 0) return;

        if (index < 0)
        {
            index = InfiniteScrolling ? _pagesCount - 1 : 0;
        }
        else if (index >= _pagesCount)
        {
            index = InfiniteScrolling ? 0 : _pagesCount - 1;
        }

        var visible = _internalVisibleItemsCount;
        var itemsCount = _allItems.Count;

        var firstIndex = index * visible;

        if (InfiniteScrolling is false)
        {
            // The last page of a carousel whose items do not divide evenly into pages is aligned to its
            // last item, so navigating to it never leaves a blank space after the item.
            firstIndex = Math.Min(firstIndex, Math.Max(0, itemsCount - visible));
        }

        if (_currentIndices[0] == firstIndex) return;

        var isNext = index > _currentPage;

        // The indices past the end wrap around to the start of an infinite carousel, so a page that is
        // only partially filled by the items borrows the missing slides from the other end instead of
        // being rendered with a blank space.
        _othersIndices = Enumerable.Range(firstIndex, visible).Select(idx =>
        {
            if (InfiniteScrolling && idx > itemsCount - 1) idx -= itemsCount;
            return idx;
        }).Where(IsValidIndex).ToArray();

        await Go(isNext, visible);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (NoKeyboard) return;
        if (IsEnabled is false) return;

        // A carousel that swallowed a modified arrow key would take the browser shortcuts of the page
        // with it, so only the plain keys are acted on.
        if (e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey) return;

        switch (e.Key)
        {
            case "ArrowRight" when Vertical is false:
            case "ArrowDown" when Vertical:
                await HandleGoLeft();
                break;

            case "ArrowLeft" when Vertical is false:
            case "ArrowUp" when Vertical:
                await HandleGoRight();
                break;

            case "Home":
                await HandleGotoPage(0);
                break;

            case "End":
                await HandleGotoPage(_pagesCount - 1);
                break;
        }
    }

    private async Task HandleWheel(WheelEventArgs e)
    {
        if (Wheel is false) return;
        if (IsEnabled is false) return;

        var delta = Math.Abs(e.DeltaY) >= Math.Abs(e.DeltaX) ? e.DeltaY : e.DeltaX;

        if (delta == 0) return;

        // A single flick of a trackpad fires a long tail of events, which would run through the whole
        // carousel at once without a quiet period between the moves.
        var now = DateTime.UtcNow;

        if ((now - _lastWheel).TotalMilliseconds < Math.Max(200, AnimationDuration * 1000)) return;

        _lastWheel = now;

        await (delta > 0 ? HandleGoLeft() : HandleGoRight());
    }

    private async Task HandlePointerMove(PointerEventArgs e)
    {
        if (_isPointerDown is false) return;

        var deltaX = e.ClientX - _pointerX;
        var deltaY = e.ClientY - _pointerY;

        var along = Vertical ? deltaY : deltaX;
        var across = Vertical ? deltaX : deltaY;

        if (Math.Abs(along) <= Math.Max(1, DragThreshold)) return;

        // A drag that travels further across the carousel than along it is a scroll of the page that
        // happens to pass over the carousel, and flipping a slide under it would be a surprise.
        if (Math.Abs(across) > Math.Abs(along))
        {
            await EndDrag();
            return;
        }

        await EndDrag();

        if (along < 0)
        {
            await HandleGoLeft();
        }
        else
        {
            await HandleGoRight();
        }
    }

    private async Task HandlePointerDown(PointerEventArgs e)
    {
        if (NoDrag) return;
        if (IsEnabled is false) return;

        _isPointerDown = true;
        _pointerX = e.ClientX;
        _pointerY = e.ClientY;

        await _js.BitUtilsSetStyle(_carouselContainer, "cursor", "grabbing");
    }

    private async Task HandlePointerUp(PointerEventArgs e)
    {
        if (_isPointerDown is false) return;

        await EndDrag();
    }

    private async Task EndDrag()
    {
        _isPointerDown = false;

        await _js.BitUtilsSetStyle(_carouselContainer, "cursor", string.Empty);
    }

    private void HandleMouseEnter()
    {
        if (PauseOnHover is false) return;

        _hovered = true;

        UpdateAutoPlayTimer();
    }

    private void HandleMouseLeave()
    {
        if (_hovered is false) return;

        _hovered = false;

        UpdateAutoPlayTimer();
    }

    private void HandleFocusIn()
    {
        if (PauseOnFocus is false) return;

        _focused = true;

        UpdateAutoPlayTimer();
    }

    private void HandleFocusOut()
    {
        if (_focused is false) return;

        _focused = false;

        UpdateAutoPlayTimer();
    }

    private bool ShouldAutoPlay()
    {
        if (AutoPlay is false) return false;
        if (IsEnabled is false) return false;
        if (_isPaused || _stopped) return false;
        if (_pageHidden) return false;
        if (PauseOnHover && _hovered) return false;
        if (PauseOnFocus && _focused) return false;

        // A carousel with a single page has nowhere to rotate to. The timer is re-evaluated from
        // ResetDimensionsAsync whenever the items (and with them the page count) change.
        return _pagesCount > 1;
    }

    private void UpdateAutoPlayTimer()
    {
        // The page visibility event can still reach a carousel that was disposed while the event was
        // being raised (the handlers are snapshotted before they are awaited), and re-creating the
        // timer here at that point would leak it, ticking, for the rest of the process.
        if (IsDisposed) return;

        // Before the first render there is nothing to rotate yet, and a timer started during
        // prerendering would tick on the server against a carousel that is not on screen.
        if (_afterFirstRender is false) return;

        if (ShouldAutoPlay() is false)
        {
            _autoPlayTimer?.Stop();
        }
        else
        {
            // The timer itself rejects an interval beyond int.MaxValue (and one that is not positive),
            // so anything unusable falls back to the default instead of throwing in the middle of a render.
            var interval = AutoPlayInterval > 0 ? Math.Min(AutoPlayInterval, int.MaxValue) : 2000;

            if (_autoPlayTimer is null)
            {
                _autoPlayTimer = new System.Timers.Timer(interval);
                _autoPlayTimer.Elapsed += AutoPlayTimerElapsed;
            }
            else if (_autoPlayTimer.Interval != interval)
            {
                _autoPlayTimer.Interval = interval;
            }

            _autoPlayTimer.Start();
        }

        // The playback state feeds the play/pause button and the aria-live attribute, so it is
        // rendered right away even when the change came from code (Pause/Resume) rather than
        // from an event the renderer already follows up on.
        StateHasChanged();
    }

    private void RestartAutoPlayTimer()
    {
        if (_autoPlayTimer is null || _autoPlayTimer.Enabled is false) return;

        _autoPlayTimer.Stop();
        _autoPlayTimer.Start();
    }

    private async void AutoPlayTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (IsDisposed) return;

        try
        {
            await InvokeAsync(async () =>
            {
                if (IsDisposed || _navigating) return;
                if (ShouldAutoPlay() is false) return;

                var forward = AutoPlayReverse is false;

                if (forward ? CanGoNext() : CanGoPrev())
                {
                    await (forward ? Next() : Prev());
                }
                else if (StopOnLastSlide)
                {
                    _stopped = true;

                    UpdateAutoPlayTimer();

                    StateHasChanged();
                }
                else
                {
                    // Rewinding keeps a carousel that was told to play going, instead of leaving it
                    // stalled on its last page for good.
                    await GotoPage(forward ? 0 : _pagesCount - 1);
                }
            });
        }
        catch (ObjectDisposedException) { } // the renderer went away between the tick and the dispatch
        catch (JSDisconnectedException) { } // the circuit went away while the slides were being moved
        catch (Exception ex)
        {
            // An exception that escapes an async void handler takes the whole process down, so anything
            // else (an OnChange callback that threw, for example) is handed back to the renderer, where
            // it is treated like an exception of any ordinary event handler.
            try
            {
                await DispatchExceptionAsync(ex);
            }
            catch { } // the renderer itself went away; there is nothing left to report the exception to
        }
    }

    private Task PageVisibilityChange(bool hidden)
    {
        _pageHidden = hidden;

        UpdateAutoPlayTimer();

        return Task.CompletedTask;
    }



    private string? GetGoLeftAriaLabel()
    {
        if (GoLeftAriaLabel.HasValue()) return GoLeftAriaLabel;

        return (Vertical is false && Dir == BitDir.Rtl) ? "Previous slide" : "Next slide";
    }

    private string? GetGoRightAriaLabel()
    {
        if (GoRightAriaLabel.HasValue()) return GoRightAriaLabel;

        return (Vertical is false && Dir == BitDir.Rtl) ? "Next slide" : "Previous slide";
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _pageVisibility.OnChange -= PageVisibilityChange;

        if (_autoPlayTimer is not null)
        {
            _autoPlayTimer.Elapsed -= AutoPlayTimerElapsed;
            _autoPlayTimer.Dispose();
            _autoPlayTimer = null;
        }

        if (_dotnetObj is not null)
        {
            //_dotnetObj.Dispose(); // it is getting disposed in the following js call:
            try
            {
                await _js.BitObserversUnregisterResize(UniqueId, RootElement, _dotnetObj);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await base.DisposeAsync(disposing);
    }
}
