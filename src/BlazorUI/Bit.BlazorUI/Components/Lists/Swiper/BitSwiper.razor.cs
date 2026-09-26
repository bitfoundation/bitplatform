using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Swipers (touch slider) let people show their slides in a swiping row.
/// </summary>
/// <remarks>
/// The items are laid out side by side inside a scrolling box, so the swiper scrolls freely through them
/// instead of paging like a <see cref="BitCarousel"/> does: the items keep whatever size they were given
/// (or take one from <see cref="VisibleItemsCount"/>), and the swiper comes to rest wherever it was left,
/// unless <see cref="Snap"/> asks it to settle on an item. It can be driven with its own next/prev
/// buttons, with the dots below it, by dragging it, with the keyboard, with the mouse wheel, from
/// application code through <see cref="GoNext"/>/<see cref="GoPrev"/>/<see cref="GoTo(int)"/>, or on its
/// own with <see cref="AutoPlay"/>.
/// <br />
/// The scrolling itself is the browser's, so a touch swipe carries the momentum and the snapping of the
/// platform it runs on, and nothing crosses over to .NET while a finger is on the screen. It follows the
/// carousel pattern of the ARIA authoring practices: the root is a labelled region marked as a carousel,
/// every item is a labelled group, the controls are real buttons, and automatic scrolling stops as soon
/// as the pointer or the keyboard focus enters the swiper.
/// </remarks>
public partial class BitSwiper : BitComponentBase
{
    private int _index;
    private int _page;
    private int _pagesCount;
    private double _viewport;
    private bool _atStart = true;
    private bool _atEnd = true;
    private bool _scrollable;

    private bool _hovered;
    private bool _stopped;
    private bool _focused;
    private bool _keysOwnedByContent;
    private bool _isPaused;
    private bool _pageHidden;
    private bool _needsSetup;
    private bool _needsRefresh;
    private bool _afterFirstRender;
    private int _laidOutItemsCount = -1;
    private (bool Playing, bool Paused) _playbackState;
    private string _optionsSignature = string.Empty;
    private int _internalScrollItemsCount = 1;
    private System.Timers.Timer? _autoPlayTimer;
    private string _directionStyle = string.Empty;
    private bool _stateReported;
    private int _reportedItemsCount;
    private bool _endReached;
    private string? _announcement;

    // Nothing is known about how far the swiper reaches until the browser has measured it, and a button
    // that flashed into view only to hide itself on the first measurement would move the items under it.
    // The initial state below (nothing to scroll, standing at both ends at once) says the same thing.
    // These are written into the same style attribute as the Styles of the consumer, so they carry their
    // own semicolon: without it the two run together into a single declaration the browser throws away.
    private string _nextButtonStyle = "display:none;";
    private string _prevButtonStyle = "display:none;";
    private readonly List<BitSwiperItem> _allItems = [];
    private ElementReference _swiperContainer = default!;
    private DotNetObjectReference<BitSwiper> _dotnetObj = default!;

    // The keys the swiper acts on are also the keys the browser scrolls the page with, so they are
    // swallowed on the way in. They are handled in the browser rather than with Blazor's preventDefault
    // directive, because the directive is static while the swiper only owns these keys while the keyboard
    // is enabled. Only the keys of the axis the swiper actually scrolls on are taken, so a horizontal
    // swiper does not keep the page from being scrolled with the up/down arrow keys.
    private static readonly string[] _horizontalNavigationKeys = ["ArrowLeft", "ArrowRight", "Home", "End", "PageUp", "PageDown"];
    private static readonly string[] _verticalNavigationKeys = ["ArrowUp", "ArrowDown", "Home", "End", "PageUp", "PageDown"];



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private BitPageVisibility _pageVisibility { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the swiper component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple swiper components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitSwiperParams.ParamName)]
    public BitSwiperParams? CascadingParameters { get; set; }



    // The element the controls of the swiper act on. Naming it lets every one of them point at it with
    // aria-controls, so the relation between a control and the items it moves is spelled out.
    internal string _ContainerId => $"{_Id}-cnt";



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
    /// It is the duration of the moves the swiper makes itself: the ones of its buttons, its dots, the
    /// keyboard, the wheel, the auto scrolling and the public API, and the glide a flick of the pointer
    /// ends with. A value of 0 moves the items at once, with no animation at all. A touch swipe is
    /// scrolled by the browser, which brings the timing of the platform with it.
    /// <br />
    /// The animation also collapses to nothing when the operating system or the browser reports
    /// 'prefers-reduced-motion: reduce', unless <see cref="BitComponentBase.ForceAnimation"/> opts the
    /// swiper out of it.
    /// </remarks>
    [Parameter] public double AnimationDuration { get; set; } = 0.5;

    /// <summary>
    /// Enables/disables the auto scrolling of the items.
    /// </summary>
    /// <remarks>
    /// The scrolling pauses on its own while the pointer is over the swiper (<see cref="PauseOnHover"/>),
    /// while the keyboard focus is inside it (<see cref="PauseOnFocus"/>) and while the page is hidden in
    /// a background tab, and it can be stopped from application code with <see cref="Pause"/> or by the
    /// person reading the page with the button <see cref="ShowPlayPause"/> renders.
    /// <br />
    /// A swiper that reaches its end rewinds to the start, unless <see cref="StopOnLastSlide"/> says to
    /// stop there.
    /// </remarks>
    [Parameter] public bool AutoPlay { get; set; }

    /// <summary>
    /// Sets the interval of the auto scrolling in milliseconds (the default value is 2000).
    /// </summary>
    [Parameter] public double AutoPlayInterval { get; set; } = 2000;

    /// <summary>
    /// Plays the auto scrolling backwards, from the last item towards the first one.
    /// </summary>
    [Parameter] public bool AutoPlayReverse { get; set; }

    /// <summary>
    /// Items of the swiper.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom CSS classes for the different parts of the swiper.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSwiperClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the swiper.
    /// </summary>
    /// <remarks>
    /// It colors the dot of the current page and the next/prev and play/pause buttons.
    /// <br />
    /// When not set, the swiper falls back to <see cref="Accent"/>, and to the primary color of the theme
    /// when that is not set either.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The item (1 based, like <see cref="GoTo(int)"/>) the swiper starts on when it first renders.
    /// </summary>
    /// <remarks>
    /// It is only read while the swiper is being set up, so changing it afterwards does not move the swiper.
    /// Use <see cref="GoTo(int)"/> for that. Values outside of the range of the swiper are clamped to its
    /// first or last item, and the swiper is rendered there rather than scrolling over to it.
    /// </remarks>
    [Parameter] public int DefaultItem { get; set; } = 1;

    /// <summary>
    /// The accessible label of a dot of the swiper, followed by the number of the page it navigates to.
    /// </summary>
    [Parameter] public string DotAriaLabel { get; set; } = "Slide";

    /// <summary>
    /// The accessible label of the dots container of the swiper.
    /// </summary>
    [Parameter] public string DotsAriaLabel { get; set; } = "Choose slide to display";

    /// <summary>
    /// The custom content of a dot of the swiper, receiving the zero based index of the page the dot
    /// navigates to.
    /// </summary>
    /// <remarks>
    /// A dot that holds content is laid out around that content instead of being rendered as the default
    /// circle, so it can carry a page number, a thumbnail, or anything else. Use <see cref="Classes"/> and
    /// <see cref="Styles"/> (their <c>Dots</c> and <c>CurrentDot</c> members) to style it.
    /// </remarks>
    [Parameter] public RenderFragment<int>? DotTemplate { get; set; }

    /// <summary>
    /// The distance (in pixels) the pointer has to travel over the swiper before it starts dragging it
    /// (the default value is 5).
    /// </summary>
    /// <remarks>
    /// A press that never travels that far is a click, and it is left to whatever it landed on, so the
    /// content of an item stays clickable and selectable.
    /// </remarks>
    [Parameter] public int DragThreshold { get; set; } = 5;

    /// <summary>
    /// The space between the items of the swiper (any CSS length, for example <c>1rem</c>).
    /// </summary>
    /// <remarks>
    /// It is also taken into account by <see cref="VisibleItemsCount"/>, so the requested number of items
    /// keeps fitting in the swiper once they are spaced out.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// Hides the Next/Prev buttons of the BitSwiper.
    /// </summary>
    /// <remarks>
    /// Each button also hides itself at the end it cannot move any further towards, and both of them are
    /// left out while everything already fits in the swiper.
    /// </remarks>
    [Parameter] public bool HideNextPrev { get; set; }

    /// <summary>
    /// The accessible label of an item of the swiper, as a composite format string whose <c>{0}</c> is the
    /// 1 based position of the item and whose <c>{1}</c> is the number of items (the default value is
    /// "{0} of {1}").
    /// </summary>
    /// <remarks>
    /// It is only used for the items that were not given an <see cref="BitComponentBase.AriaLabel"/> of
    /// their own, which is what the carousel pattern of the ARIA authoring practices asks for when a slide
    /// has nothing better to be called.
    /// <br />
    /// The accessible name of an item (its own, or the one this format gives it) is also what the swiper
    /// announces to screen readers when it comes to stand on that item, except while it plays on its own.
    /// </remarks>
    [Parameter] public string? ItemAriaLabelFormat { get; set; }

    /// <summary>
    /// The accessible label of the next button of the swiper (the default value is "Next slide").
    /// </summary>
    [Parameter] public string? NextAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the next navigation button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="NextIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? NextIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the next navigation button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChevronRight</c>).
    /// <br />
    /// For external icon libraries, use <see cref="NextIcon"/> instead.
    /// </remarks>
    [Parameter] public string? NextIconName { get; set; }

    /// <summary>
    /// Disables dragging the swiper with the mouse.
    /// </summary>
    /// <remarks>
    /// A touch swipe is the browser's own scrolling of the region, which is not taken away.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoDrag { get; set; }

    /// <summary>
    /// Removes the swiper from the tab sequence and turns off its keyboard navigation.
    /// </summary>
    /// <remarks>
    /// A swiper is normally focusable so it can be operated with the arrow keys (and with Home, End,
    /// PageUp and PageDown), which is the only way to reach an item for someone who cannot use a pointer.
    /// Only turn this off when the same navigation is offered somewhere else on the page.
    /// </remarks>
    [Parameter] public bool NoKeyboard { get; set; }

    /// <summary>
    /// The event that will be called when the swiper comes to stand on another item.
    /// </summary>
    /// <remarks>
    /// The provided value is the zero based index of the item the swiper moved to, however it was moved:
    /// with its controls, by dragging, by scrolling it, with the keyboard or by the auto scrolling.
    /// </remarks>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// The event that will be called when the swiper is scrolled all the way to its end.
    /// </summary>
    /// <remarks>
    /// It fires each time the swiper is moved to its end, not while it stays there, and not when the end
    /// comes to it instead: a resize clamping it to a shorter reach, or items taken out while it stands near
    /// the end. A swiper everything fits in has its end in view without going anywhere, so it fires for that
    /// too, once per set of items. That makes it the place to load more items: once they are added, the
    /// swiper either has somewhere to go again and fires the next time it gets there, or still fits them all
    /// and fires straight away.
    /// </remarks>
    [Parameter] public EventCallback OnReachEnd { get; set; }

    /// <summary>
    /// The event that will be called when the swiper is scrolled all the way back to its start.
    /// </summary>
    /// <remarks>
    /// It fires each time the swiper is moved back to its start, not for the start it is first laid out on,
    /// and not when a resize or a change of its items puts it there.
    /// </remarks>
    [Parameter] public EventCallback OnReachStart { get; set; }

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
    /// Pauses the auto scrolling while the keyboard focus is inside the swiper (the default value is true).
    /// </summary>
    /// <remarks>
    /// Turning this off makes the swiper move the control that is being operated with the keyboard out of
    /// the view, so leave it on unless the items hold nothing focusable at all.
    /// </remarks>
    [Parameter] public bool PauseOnFocus { get; set; } = true;

    /// <summary>
    /// Pauses the auto scrolling while the pointer is over the swiper (the default value is true).
    /// </summary>
    [Parameter] public bool PauseOnHover { get; set; } = true;

    /// <summary>
    /// The room (any CSS length, for example <c>2rem</c>) kept at both ends of the swiper, which the items
    /// next to the ones in view peek into.
    /// </summary>
    /// <remarks>
    /// A partly shown item is the clearest hint that there is more to scroll to. The room is taken out of
    /// the swiper before <see cref="VisibleItemsCount"/> sizes its items, so the requested number of them
    /// still fits whole between the two ends, and the items settle against it rather than against the edge
    /// of the swiper, both when it navigates and when it snaps. Each end is capped at a quarter of the
    /// swiper, so half of it is always left for the items however large a peek is asked for.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Peek { get; set; }

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
    /// The accessible label of the previous button of the swiper (the default value is "Previous slide").
    /// </summary>
    [Parameter] public string? PrevAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the previous navigation button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PrevIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="PrevIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? PrevIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the previous navigation button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChevronLeft</c>).
    /// <br />
    /// For external icon libraries, use <see cref="PrevIcon"/> instead.
    /// </remarks>
    [Parameter] public string? PrevIconName { get; set; }

    /// <summary>
    /// Wraps the manual navigation around: moving on from the end of the swiper goes back to its start, and
    /// moving back from its start goes to its end.
    /// </summary>
    /// <remarks>
    /// It covers the next/prev buttons (which then stay visible at both ends), the arrow keys and
    /// <see cref="GoNext"/>/<see cref="GoPrev"/>. The wheel and the dragging stop at the ends either way, since a
    /// scroll that jumps back to the start under the hand reads as a glitch rather than as a navigation.
    /// </remarks>
    [Parameter] public bool Rewind { get; set; }

    /// <summary>
    /// Number of items that is going to be changed on navigation.
    /// </summary>
    /// <remarks>
    /// It is the step of the next/prev buttons, of the arrow keys and of the mouse wheel. A move that would
    /// overshoot the end of the swiper runs out to it instead, so the last item is never left half shown.
    /// </remarks>
    [Parameter] public int ScrollItemsCount { get; set; } = 1;

    /// <summary>
    /// Renders the navigation dots below the items of the swiper.
    /// </summary>
    /// <remarks>
    /// A dot stands for a screenful of the swiper rather than for a single item, since a swiper shows as
    /// many items as it has room for. They are left out while everything already fits in the swiper.
    /// </remarks>
    [Parameter] public bool ShowDots { get; set; }

    /// <summary>
    /// Renders a play/pause button next to the dots, so the auto scrolling can be stopped and started again.
    /// </summary>
    /// <remarks>
    /// Only rendered while <see cref="AutoPlay"/> is enabled. Offering this control is what the "pause,
    /// stop, hide" success criterion of WCAG asks of any content that moves on its own for more than five
    /// seconds, so turn it on for a swiper that carries meaningful content.
    /// </remarks>
    [Parameter] public bool ShowPlayPause { get; set; }

    /// <summary>
    /// Leaves the scrollbar of the swiper visible, which is hidden by default.
    /// </summary>
    /// <remarks>
    /// The swiper is a real scrolling region, so its scrollbar is another way of moving it (and, on the
    /// platforms that always draw one, a hint that there is more to be seen).
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ShowScrollbar { get; set; }

    /// <summary>
    /// The size of the dots and of the next/prev buttons of the swiper.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Settles the swiper on an item instead of leaving it wherever the scrolling ran out.
    /// </summary>
    /// <remarks>
    /// The value chooses where the item it settles on comes to rest: at the start of the swiper, in its
    /// middle, or at its end. Without it the swiper scrolls freely, which is what a rail of items that are
    /// read side by side wants.
    /// <br />
    /// The snapping itself is the browser's (CSS scroll snapping), so a touch swipe settles the same way a
    /// navigation does.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSwiperSnap? Snap { get; set; }

    /// <summary>
    /// Stops the auto scrolling as soon as the swiper is navigated through one of its own controls.
    /// </summary>
    /// <remarks>
    /// This covers the next/prev buttons, the dots, and the keyboard navigation (the arrow, page, home and
    /// end keys); dragging the items and scrolling the wheel over them are handled by the browser side of
    /// the swiper and do not stop the auto scrolling. Once stopped this way the scrolling only comes back
    /// through <see cref="Resume"/> or the play/pause button, which is what someone who took over the
    /// swiper expects.
    /// </remarks>
    [Parameter] public bool StopOnInteraction { get; set; }

    /// <summary>
    /// Stops the auto scrolling at the end of the swiper instead of rewinding to its start.
    /// </summary>
    [Parameter] public bool StopOnLastSlide { get; set; }

    /// <summary>
    /// The custom CSS styles for the different parts of the swiper.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitSwiperClassStyles? Styles { get; set; }

    /// <summary>
    /// Stacks the items vertically, so the swiper scrolls up and down instead of left and right.
    /// </summary>
    /// <remarks>
    /// A vertical swiper is laid out over the height of its root element, so give it one (the default is a
    /// fixed height, and any height set through <see cref="BitComponentBase.Style"/> or a class replaces
    /// it). Its direction does not flip in right-to-left, since only the horizontal axis has a direction.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }

    /// <summary>
    /// Number of items that is visible in the swiper, which sizes the items accordingly.
    /// </summary>
    /// <remarks>
    /// Without it the items keep whatever size they were given, which is what a rail of differently sized
    /// items wants; with it they are all sized to a fraction of the swiper (with <see cref="Gap"/> taken
    /// into account), which is what a row of cards wants.
    /// <br />
    /// The XS to XXL variants of the parameter override it responsively, based on the size the swiper
    /// itself is laid out at along the axis it scrolls on (not the size of the viewport). They apply from
    /// their breakpoint upwards, so a swiper that only sets a couple of them keeps the value of the largest
    /// one below its size, and falls back to this parameter below the smallest one. The breakpoints are the
    /// shared ones of bit BlazorUI: 600, 960, 1280, 1920 and 2560 pixels.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? VisibleItemsCount { get; set; }

    /// <summary>
    /// Number of visible items in the extra small breakpoint (from 0 up, see <see cref="VisibleItemsCount"/>).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? VisibleItemsCountXs { get; set; }

    /// <summary>
    /// Number of visible items in the small breakpoint (from 600px up, see <see cref="VisibleItemsCount"/>).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? VisibleItemsCountSm { get; set; }

    /// <summary>
    /// Number of visible items in the medium breakpoint (from 960px up, see <see cref="VisibleItemsCount"/>).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? VisibleItemsCountMd { get; set; }

    /// <summary>
    /// Number of visible items in the large breakpoint (from 1280px up, see <see cref="VisibleItemsCount"/>).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? VisibleItemsCountLg { get; set; }

    /// <summary>
    /// Number of visible items in the extra large breakpoint (from 1920px up, see <see cref="VisibleItemsCount"/>).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? VisibleItemsCountXl { get; set; }

    /// <summary>
    /// Number of visible items in the extra extra large breakpoint (from 2560px up, see <see cref="VisibleItemsCount"/>).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? VisibleItemsCountXxl { get; set; }

    /// <summary>
    /// Navigates the swiper with the wheel of the mouse (or with a two finger scroll on a trackpad).
    /// </summary>
    /// <remarks>
    /// The swiper keeps the wheel to itself while this is enabled, so the page underneath it does not
    /// scroll while the pointer is over the swiper. Rolling the wheel away from the reader (down) moves
    /// the swiper forwards, whichever way its items are laid out.
    /// <br />
    /// A scroll that already runs along the swiper (a trackpad swiped sideways over a horizontal swiper)
    /// is the browser's own scrolling of the region and is left alone either way.
    /// </remarks>
    [Parameter] public bool Wheel { get; set; }



    /// <summary>
    /// The zero based index of the item the swiper is currently standing on.
    /// </summary>
    public int CurrentIndex => _index;

    /// <summary>
    /// The zero based index of the screenful (page) the swiper is currently showing.
    /// </summary>
    public int CurrentPage => _page;

    /// <summary>
    /// Whether the swiper is scrolled all the way to its start.
    /// </summary>
    public bool IsAtStart => _atStart;

    /// <summary>
    /// Whether the swiper is scrolled all the way to its end.
    /// </summary>
    public bool IsAtEnd => _atEnd;

    /// <summary>
    /// Whether the auto scrolling has been paused through <see cref="Pause"/> or the play/pause button.
    /// </summary>
    public bool IsPaused => _isPaused || _stopped;

    /// <summary>
    /// Whether the auto scrolling is currently running.
    /// </summary>
    /// <remarks>
    /// It is false while the scrolling is paused for any reason, including the pointer resting over the
    /// swiper and the page sitting in a background tab.
    /// </remarks>
    public bool IsPlaying => _autoPlayTimer?.Enabled ?? false;

    /// <summary>
    /// The number of items of the swiper.
    /// </summary>
    public int ItemsCount => _allItems.Count;

    /// <summary>
    /// The number of screenfuls (pages) the items of the swiper take up.
    /// </summary>
    public int PagesCount => _pagesCount;



    /// <summary>
    /// Navigates to the next swiper item (back to the first one from the end when <see cref="Rewind"/> is enabled).
    /// </summary>
    public async Task GoNext() => await Go(true);

    /// <summary>
    /// Navigates to the previous swiper item (on to the last one from the start when <see cref="Rewind"/> is enabled).
    /// </summary>
    public async Task GoPrev() => await Go(false);

    /// <summary>
    /// Navigates to the given swiper item number (1 based).
    /// </summary>
    /// <param name="number">
    /// The 1 based number of the item to navigate to. Values outside of the range of the swiper are clamped
    /// to its first or last item.
    /// </param>
    public async Task GoTo(int number)
    {
        if (IsDisposed || IsEnabled is false || _afterFirstRender is false || _allItems.Count == 0) return;

        var index = Math.Clamp(number - 1, 0, _allItems.Count - 1);

        await _js.BitSwiperGoToItem(_Id, index);
    }

    /// <summary>
    /// Navigates to the given swiper page number (1 based), a page being one screenful of the swiper.
    /// </summary>
    /// <param name="number">
    /// The 1 based number of the page to navigate to. Values outside of the range of the swiper are clamped
    /// to its first or last page.
    /// </param>
    public async Task GoToPage(int number)
    {
        if (IsDisposed || IsEnabled is false || _pagesCount < 1) return;

        await _js.BitSwiperGoToPage(_Id, Math.Clamp(number - 1, 0, _pagesCount - 1));
    }

    /// <summary>
    /// Navigates to the start of the swiper.
    /// </summary>
    public async Task GoToStart()
    {
        if (IsDisposed || IsEnabled is false || _afterFirstRender is false) return;

        await _js.BitSwiperGoToEdge(_Id, false);
    }

    /// <summary>
    /// Navigates to the end of the swiper.
    /// </summary>
    public async Task GoToEnd()
    {
        if (IsDisposed || IsEnabled is false || _afterFirstRender is false) return;

        await _js.BitSwiperGoToEdge(_Id, true);
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
    /// Measures the swiper again and reports where it stands.
    /// </summary>
    /// <remarks>
    /// The swiper already does this on its own whenever it or one of its items is resized and whenever its
    /// items change, so this is only needed after something else moved the ground under it.
    /// </remarks>
    public async Task Refresh()
    {
        if (IsDisposed || _afterFirstRender is false) return;

        await _js.BitSwiperRefresh(_Id);
    }



    [JSInvokable("OnStateChange")]
    public async Task _OnStateChange(BitSwiperState state)
    {
        if (IsDisposed || state is null) return;

        var previousIndex = _index;
        var wasAtStart = _atStart;
        var wasAtEnd = _atEnd;

        // A move is a report following another one over the same box and the same items, so only the scroll
        // position changed. A resize clamping the swiper to a shorter reach, or items taken out from under
        // it, can leave it at an end too, but it went nowhere to get there.
        var moved = _stateReported &&
                    Math.Abs(_viewport - state.Viewport) <= 0.5 &&
                    _reportedItemsCount == _allItems.Count;

        // Another set of items is another end to reach, and leaving the end makes it one to reach again.
        if (_reportedItemsCount != _allItems.Count || state.AtEnd is false)
        {
            _endReached = false;
        }

        _reportedItemsCount = _allItems.Count;

        _index = state.Index;
        _page = state.Page;
        _pagesCount = state.PagesCount;
        _atStart = state.AtStart;
        _atEnd = state.AtEnd;
        _scrollable = state.Scrollable;

        // The size the responsive variants of VisibleItemsCount are matched against is the size of the box
        // the items scroll in, along the axis they scroll on, which is exactly what is reported here.
        if (Math.Abs(_viewport - state.Viewport) > 0.5)
        {
            _viewport = state.Viewport;

            StyleBuilder.Reset();
        }

        SetNavigationButtonsVisibility();
        UpdateItemsCurrentState();

        // Scrolling the swiper changes nothing in the DOM a screen reader would pick up on its own, so the
        // item it comes to stand on is announced through a live region. The first report only says where
        // the swiper was laid out, which is not news, so it is not announced.
        if (_stateReported && previousIndex != _index && _index >= 0 && _index < _allItems.Count)
        {
            _announcement = _allItems[_index].GetAriaLabel();
        }

        _stateReported = true;

        // The end of the swiper is what the auto scrolling hinges on, so the timer is re-evaluated here: a
        // swiper that only just received enough items to have somewhere to go starts, and one whose items
        // were taken away stops.
        UpdateAutoPlayTimer();

        StateHasChanged();

        if (previousIndex != _index)
        {
            await OnChange.InvokeAsync(_index);

            // The handler may have taken the swiper away (navigating off the page, for one).
            if (IsDisposed) return;
        }

        // Only a move counts at the start: the place the swiper is first laid out on is not one.
        if (moved && _scrollable && wasAtStart is false && _atStart)
        {
            await OnReachStart.InvokeAsync();

            if (IsDisposed) return;
        }

        // The end is reached by moving to it or, in a swiper everything fits in, by having it in view already;
        // without the latter, a swiper loading more items here would stall on a first batch that fits.
        if (_atEnd && _endReached is false && (_scrollable is false || (moved && wasAtEnd is false)))
        {
            _endReached = true;

            await OnReachEnd.InvokeAsync();
        }
    }



    // Reported from the browser when the keyboard focus moves onto (or away from) something inside an item that
    // takes the navigation keys for itself: a text field, a select, a slider, a listbox, and the like. The arrow
    // keys belong to that control then, so the swiper leaves them alone instead of moving under the caret.
    [JSInvokable("OnKeysOwnerChange")]
    public void _OnKeysOwnerChange(bool ownedByContent)
    {
        _keysOwnedByContent = ownedByContent;
    }



    internal void RegisterItem(BitSwiperItem item)
    {
        item.Index = _allItems.Count;

        _allItems.Add(item);

        StateHasChanged();
    }

    internal void UnregisterItem(BitSwiperItem item)
    {
        if (_allItems.Remove(item) is false) return;

        // The indices of the items are what the accessible names and the current item marker are written
        // in terms of, so the ones after the removed item are moved up rather than left pointing one slot
        // too far.
        for (int i = 0; i < _allItems.Count; i++)
        {
            _allItems[i].Index = i;
        }

        if (IsDisposed) return;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-swp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Vertical ? "bit-swp-vrt" : string.Empty);

        ClassBuilder.Register(() => ShowScrollbar ? "bit-swp-scb" : string.Empty);

        ClassBuilder.Register(() => (NoDrag || IsEnabled is false) ? "bit-swp-ndr" : string.Empty);

        ClassBuilder.Register(() => Snap switch
        {
            BitSwiperSnap.Start => "bit-swp-snp bit-swp-sns",
            BitSwiperSnap.Center => "bit-swp-snp bit-swp-snc",
            BitSwiperSnap.End => "bit-swp-snp bit-swp-sne",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-swp-sm",
            BitSize.Medium => "bit-swp-md",
            BitSize.Large => "bit-swp-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Accent switch
        {
            BitColorKind.Primary => "bit-swp-apri",
            BitColorKind.Secondary => "bit-swp-asec",
            BitColorKind.Tertiary => "bit-swp-ater",
            BitColorKind.Transparent => "bit-swp-atra",
            _ => "bit-swp-apri"
        });

        // The color classes come after the accent ones in the stylesheet, so a swiper that sets both ends
        // up with the color it was given rather than with the accent it fell back to.
        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-swp-pri",
            BitColor.Secondary => "bit-swp-sec",
            BitColor.Tertiary => "bit-swp-ter",
            BitColor.Info => "bit-swp-inf",
            BitColor.Success => "bit-swp-suc",
            BitColor.Warning => "bit-swp-wrn",
            BitColor.SevereWarning => "bit-swp-swr",
            BitColor.Error => "bit-swp-err",
            BitColor.PrimaryBackground => "bit-swp-pbg",
            BitColor.SecondaryBackground => "bit-swp-sbg",
            BitColor.TertiaryBackground => "bit-swp-tbg",
            BitColor.PrimaryForeground => "bit-swp-pfg",
            BitColor.SecondaryForeground => "bit-swp-sfg",
            BitColor.TertiaryForeground => "bit-swp-tfg",
            BitColor.PrimaryBorder => "bit-swp-pbr",
            BitColor.SecondaryBorder => "bit-swp-sbr",
            BitColor.TertiaryBorder => "bit-swp-tbr",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Gap.HasValue() ? $"--bit-swp-gap:{Gap}" : string.Empty);

        StyleBuilder.Register(() => Peek.HasValue() ? $"--bit-swp-peek:{Peek}" : string.Empty);

        // The size the items are given is handed to the stylesheet as a variable rather than written onto
        // every one of them, so a swiper that changes how many items it shows costs one style on the root
        // instead of one round trip per item.
        StyleBuilder.Register(() =>
        {
            var count = ResolveVisibleItemsCount();

            if (count is null) return string.Empty;

            return count.Value < 2
                ? "--bit-swp-isz:100%"
                : FormattableString.Invariant($"--bit-swp-isz:calc((100% - {count.Value - 1} * var(--bit-swp-gap, 0px)) / {count.Value})");
        });
    }

    protected override void OnInitialized()
    {
        _pageVisibility.OnChange += PageVisibilityChange;

        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSwiperParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        _directionStyle = Dir == BitDir.Rtl ? "direction:rtl;" : string.Empty;

        _internalScrollItemsCount = Math.Max(1, ScrollItemsCount);

        // Rewind decides whether the buttons stay at the ends, so they are re-evaluated with the parameters.
        SetNavigationButtonsVisibility();

        // Everything the browser side of the swiper is driven with is folded into one signature, so a
        // single comparison decides whether it has to be told about a change at all.
        var signature = ComputeOptionsSignature();

        if (_optionsSignature != signature)
        {
            _optionsSignature = signature;

            _needsSetup = _afterFirstRender;
        }

        UpdateAutoPlayTimer();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsDisposed) return;

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            _afterFirstRender = true;
            _needsSetup = false;
            _laidOutItemsCount = _allItems.Count;

            await _js.BitSwiperSetup(_Id, RootElement, _swiperContainer, _dotnetObj, GetOptions());

            await RegisterPreventKeysAsync();

            await _pageVisibility.Init();

            UpdateAutoPlayTimer();
        }
        else
        {
            if (_needsSetup)
            {
                _needsSetup = false;

                await _js.BitSwiperUpdate(_Id, GetOptions());

                await RegisterPreventKeysAsync();
            }

            // An item that was added or taken away changes how far the swiper reaches, which its observer
            // in the browser cannot see: the box the items scroll in keeps its own size either way.
            if (_laidOutItemsCount != _allItems.Count)
            {
                _laidOutItemsCount = _allItems.Count;
                _needsRefresh = true;

                // The accessible name an item falls back to counts the items around it, so all of them are
                // rendered again when the swiper gains or loses one.
                foreach (var item in _allItems)
                {
                    item.Refresh();
                }
            }

            if (_needsRefresh)
            {
                _needsRefresh = false;

                await _js.BitSwiperRefresh(_Id);
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private BitSwiperOptions GetOptions()
    {
        return new BitSwiperOptions
        {
            Vertical = Vertical,
            NoDrag = NoDrag,
            Wheel = Wheel,
            Enabled = IsEnabled,
            Snap = Snap is not null,
            Align = Snap switch
            {
                BitSwiperSnap.Center => 0.5,
                BitSwiperSnap.End => 1,
                _ => 0
            },
            Duration = Math.Max(0, AnimationDuration),
            Threshold = Math.Max(1, DragThreshold),
            Rewind = Rewind,
            ScrollCount = _internalScrollItemsCount,

            // Only read while the swiper is being set up, so an update that carries it along never moves a
            // swiper the reader has already scrolled somewhere else.
            Start = Math.Max(0, DefaultItem - 1)
        };
    }

    private string ComputeOptionsSignature()
    {
        return FormattableString.Invariant(
            $"{Vertical}|{NoDrag}|{Wheel}|{IsEnabled}|{Snap}|{AnimationDuration}|{DragThreshold}|{_internalScrollItemsCount}|{NoKeyboard}|{Rewind}");
    }

    private async Task RegisterPreventKeysAsync()
    {
        var keys = (NoKeyboard || IsEnabled is false) ? [] : (Vertical ? _verticalNavigationKeys : _horizontalNavigationKeys);

        await _js.BitUtilsRegisterPreventKeys(RootElement, keys);
    }

    // The responsive variants apply from their breakpoint (of the size of the swiper itself) upwards, like
    // the columns of BitGrid, so the one that wins is the largest breakpoint the swiper has grown past
    // that was actually set. Before the first measurement (and on prerendering, where there is nothing to
    // measure) the base value is used.
    private int? ResolveVisibleItemsCount()
    {
        int? resolved = VisibleItemsCount;

        if (_viewport > 0)
        {
            int? responsive = VisibleItemsCountXs;

            if (_viewport >= BitBreakpoints.Sm) responsive = VisibleItemsCountSm ?? responsive;
            if (_viewport >= BitBreakpoints.Md) responsive = VisibleItemsCountMd ?? responsive;
            if (_viewport >= BitBreakpoints.Lg) responsive = VisibleItemsCountLg ?? responsive;
            if (_viewport >= BitBreakpoints.Xl) responsive = VisibleItemsCountXl ?? responsive;
            if (_viewport >= BitBreakpoints.Xxl) responsive = VisibleItemsCountXxl ?? responsive;

            resolved = responsive ?? resolved;
        }

        return resolved is null ? null : Math.Max(1, resolved.Value);
    }

    private string GetDefaultNextIconName()
    {
        if (Vertical) return "ChevronDown";

        return Dir == BitDir.Rtl ? "ChevronRight bit-ico-r180" : "ChevronRight";
    }

    private string GetDefaultPrevIconName()
    {
        if (Vertical) return "ChevronUp";

        return Dir == BitDir.Rtl ? "ChevronRight" : "ChevronRight bit-ico-r180";
    }

    private void SetNavigationButtonsVisibility()
    {
        // A swiper everything already fits in has nowhere to go, so neither button is of any use on it. A
        // rewinding one always has somewhere to go, so it keeps both of them at its ends.
        _nextButtonStyle = (_scrollable is false || (_atEnd && Rewind is false)) ? "display:none;" : string.Empty;
        _prevButtonStyle = (_scrollable is false || (_atStart && Rewind is false)) ? "display:none;" : string.Empty;
    }

    private void UpdateItemsCurrentState()
    {
        for (int i = 0; i < _allItems.Count; i++)
        {
            _allItems[i].InternalIsCurrent = i == _index;
        }
    }

    private async Task Go(bool forward, int? count = null)
    {
        if (IsDisposed || IsEnabled is false || _afterFirstRender is false) return;

        if (Rewind && _scrollable && (forward ? _atEnd : _atStart))
        {
            await _js.BitSwiperGoToEdge(_Id, forward is false);
            return;
        }

        await _js.BitSwiperGo(_Id, forward, count ?? _internalScrollItemsCount);
    }

    // Everything the person reading the page does to the swiper by hand goes through here first, so the
    // auto scrolling can step aside for them when it was asked to.
    private void HandleInteraction()
    {
        if (StopOnInteraction is false) return;
        if (AutoPlay is false) return;

        _stopped = true;

        UpdateAutoPlayTimer();
    }

    private async Task HandleGoNext()
    {
        HandleInteraction();

        await GoNext();
    }

    private async Task HandleGoPrev()
    {
        HandleInteraction();

        await GoPrev();
    }

    private async Task HandleGoToPage(int index)
    {
        HandleInteraction();

        if (IsDisposed || IsEnabled is false) return;

        await _js.BitSwiperGoToPage(_Id, index);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (NoKeyboard) return;
        if (IsEnabled is false) return;
        if (_keysOwnedByContent) return;

        // A swiper that swallowed a modified arrow key would take the browser shortcuts of the page with
        // it, so only the plain keys are acted on.
        if (e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey) return;

        // The arrow keys move the view the way they point, which is what they do in every scrolling
        // region: the right arrow reveals what sits on the right, and in a right-to-left swiper that is
        // the item before the current one.
        var rtl = Vertical is false && Dir == BitDir.Rtl;

        switch (e.Key)
        {
            case "ArrowRight" when Vertical is false:
            case "ArrowDown" when Vertical:
                HandleInteraction();
                await Go(rtl is false);
                break;

            case "ArrowLeft" when Vertical is false:
            case "ArrowUp" when Vertical:
                HandleInteraction();
                await Go(rtl);
                break;

            case "PageDown":
                await HandleGoToPage(_page + 1);
                break;

            case "PageUp":
                await HandleGoToPage(_page - 1);
                break;

            case "Home":
                HandleInteraction();
                await GoToStart();
                break;

            case "End":
                HandleInteraction();
                await GoToEnd();
                break;
        }
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

        // A swiper everything fits in has nowhere to scroll to.
        return _scrollable;
    }

    private void UpdateAutoPlayTimer()
    {
        // The page visibility event can still reach a swiper that was disposed while the event was being
        // raised (the handlers are snapshotted before they are awaited), and re-creating the timer here at
        // that point would leak it, ticking, for the rest of the process.
        if (IsDisposed) return;

        // Before the first render there is nothing to scroll yet, and a timer started during prerendering
        // would tick on the server against a swiper that is not on screen.
        if (_afterFirstRender is false) return;

        if (ShouldAutoPlay() is false)
        {
            _autoPlayTimer?.Stop();
        }
        else
        {
            // The timer itself rejects an interval beyond int.MaxValue (and one that is not positive), so
            // anything unusable falls back to the default instead of throwing in the middle of a render.
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

        // The playback state feeds the play/pause button, so it is rendered right away even when the
        // change came from code (Pause/Resume) rather than from an event the renderer already follows up
        // on. Nothing else here shows, so a state that did not move (this runs on every parameter set) is
        // not worth a render.
        var state = (IsPlaying, IsPaused);

        if (_playbackState == state) return;

        _playbackState = state;

        StateHasChanged();
    }

    private async void AutoPlayTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (IsDisposed) return;

        try
        {
            await InvokeAsync(async () =>
            {
                if (IsDisposed) return;
                if (ShouldAutoPlay() is false) return;

                var forward = AutoPlayReverse is false;

                if (forward ? (_atEnd is false) : (_atStart is false))
                {
                    await Go(forward);
                }
                else if (StopOnLastSlide)
                {
                    _stopped = true;

                    UpdateAutoPlayTimer();

                    StateHasChanged();
                }
                else
                {
                    // Rewinding keeps a swiper that was told to play going, instead of leaving it stalled
                    // at its end for good.
                    await (forward ? GoToStart() : GoToEnd());
                }
            });
        }
        catch (ObjectDisposedException) { } // the renderer went away between the tick and the dispatch
        catch (JSDisconnectedException) { } // the circuit went away while the items were being moved
        catch (Exception ex)
        {
            // An exception that escapes an async void handler takes the whole process down, so anything
            // else (an OnChange callback that threw, for example) is handed back to the renderer, where it
            // is treated like an exception of any ordinary event handler.
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
                await _js.BitSwiperDispose(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await base.DisposeAsync(disposing);
    }
}
