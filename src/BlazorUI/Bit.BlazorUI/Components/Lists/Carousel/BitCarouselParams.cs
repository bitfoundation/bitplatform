namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCarousel"/> component.
/// </summary>
public class BitCarouselParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCarousel"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCarousel value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCarousel)}";



    public string Name => ParamName;



    /// <summary>
    /// The accent color kind of the carousel, which colors the dot of the current page.
    /// </summary>
    public BitColorKind? Accent { get; set; }

    /// <summary>
    /// The duration of the scrolling animation in seconds.
    /// </summary>
    public double? AnimationDuration { get; set; }

    /// <summary>
    /// Enables/disables the auto scrolling of the slides.
    /// </summary>
    public bool? AutoPlay { get; set; }

    /// <summary>
    /// The interval of the auto scrolling in milliseconds.
    /// </summary>
    public double? AutoPlayInterval { get; set; }

    /// <summary>
    /// Plays the auto scrolling backwards, from the last slide towards the first one.
    /// </summary>
    public bool? AutoPlayReverse { get; set; }

    /// <summary>
    /// The custom CSS classes for the different parts of the carousel.
    /// </summary>
    public BitCarouselClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the carousel, which colors the dot of the current page and the next/prev and play/pause buttons.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The accessible label of a dot of the carousel, followed by the number of the page it navigates to.
    /// </summary>
    public string? DotAriaLabel { get; set; }

    /// <summary>
    /// The accessible label of the dots container of the carousel.
    /// </summary>
    public string? DotsAriaLabel { get; set; }

    /// <summary>
    /// Where the dots (and the play/pause button) are placed around the slides.
    /// </summary>
    public BitCarouselDotsPosition? DotsPosition { get; set; }

    /// <summary>
    /// The custom content of a dot of the carousel, receiving the zero based index of the page the dot navigates to.
    /// </summary>
    public RenderFragment<int>? DotTemplate { get; set; }

    /// <summary>
    /// The distance (in pixels) the pointer has to travel over the carousel before it moves to another page.
    /// </summary>
    public int? DragThreshold { get; set; }

    /// <summary>
    /// Cross-fades the slides in place instead of sliding them.
    /// </summary>
    public bool? Fade { get; set; }

    /// <summary>
    /// The space between the slides of the carousel (any CSS length, for example <c>1rem</c>).
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// The accessible label of the go to left button of the carousel.
    /// </summary>
    public string? GoLeftAriaLabel { get; set; }

    /// <summary>
    /// The icon of the go to left button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="GoLeftIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? GoLeftIcon { get; set; }

    /// <summary>
    /// The name of the icon of the go to left button from the built-in Fluent UI icons.
    /// </summary>
    public string? GoLeftIconName { get; set; }

    /// <summary>
    /// The accessible label of the go to right button of the carousel.
    /// </summary>
    public string? GoRightAriaLabel { get; set; }

    /// <summary>
    /// The icon of the go to right button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="GoRightIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? GoRightIcon { get; set; }

    /// <summary>
    /// The name of the icon of the go to right button from the built-in Fluent UI icons.
    /// </summary>
    public string? GoRightIconName { get; set; }

    /// <summary>
    /// Hides the dots indicator of the carousel.
    /// </summary>
    public bool? HideDots { get; set; }

    /// <summary>
    /// Hides the next/prev buttons of the carousel.
    /// </summary>
    public bool? HideNextPrev { get; set; }

    /// <summary>
    /// Navigates the carousel items in an infinite loop.
    /// </summary>
    public bool? InfiniteScrolling { get; set; }

    /// <summary>
    /// The accessible label of a slide that has none of its own, as a composite format string
    /// (<c>{0}</c> is the 1 based position of the slide and <c>{1}</c> the number of slides).
    /// </summary>
    public string? ItemAriaLabelFormat { get; set; }

    /// <summary>
    /// Disables dragging the carousel with the pointer.
    /// </summary>
    public bool? NoDrag { get; set; }

    /// <summary>
    /// Removes the carousel from the tab sequence and turns off its keyboard navigation.
    /// </summary>
    public bool? NoKeyboard { get; set; }

    /// <summary>
    /// The accessible label of the play/pause button while the auto scrolling is running.
    /// </summary>
    public string? PauseButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the play/pause button while the auto scrolling is running, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? PauseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the play/pause button while the auto scrolling is running, from the built-in Fluent UI icons.
    /// </summary>
    public string? PauseIconName { get; set; }

    /// <summary>
    /// Pauses the auto scrolling while the keyboard focus is inside the carousel.
    /// </summary>
    public bool? PauseOnFocus { get; set; }

    /// <summary>
    /// Pauses the auto scrolling while the pointer is over the carousel.
    /// </summary>
    public bool? PauseOnHover { get; set; }

    /// <summary>
    /// The accessible label of the play/pause button while the auto scrolling is paused.
    /// </summary>
    public string? PlayButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the play/pause button while the auto scrolling is paused, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? PlayIcon { get; set; }

    /// <summary>
    /// The name of the icon of the play/pause button while the auto scrolling is paused, from the built-in Fluent UI icons.
    /// </summary>
    public string? PlayIconName { get; set; }

    /// <summary>
    /// Adapts the visible and scroll items counts to the width of the carousel.
    /// </summary>
    public IEnumerable<BitCarouselResponsiveOption>? ResponsiveOptions { get; set; }

    /// <summary>
    /// Number of items that is going to be changed on navigation.
    /// </summary>
    public int? ScrollItemsCount { get; set; }

    /// <summary>
    /// Renders a play/pause button next to the dots while the auto scrolling is enabled.
    /// </summary>
    public bool? ShowPlayPause { get; set; }

    /// <summary>
    /// The size of the dots and of the next/prev buttons of the carousel.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Stops the auto scrolling as soon as the carousel is navigated by hand.
    /// </summary>
    public bool? StopOnInteraction { get; set; }

    /// <summary>
    /// Stops the auto scrolling on the last page instead of rewinding to the first one.
    /// </summary>
    public bool? StopOnLastSlide { get; set; }

    /// <summary>
    /// The custom CSS styles for the different parts of the carousel.
    /// </summary>
    public BitCarouselClassStyles? Styles { get; set; }

    /// <summary>
    /// Stacks the slides vertically, so the carousel scrolls up and down instead of left and right.
    /// </summary>
    public bool? Vertical { get; set; }

    /// <summary>
    /// Number of items that is visible in the carousel.
    /// </summary>
    public int? VisibleItemsCount { get; set; }

    /// <summary>
    /// Number of visible items in the extra small breakpoint (from 0 up).
    /// </summary>
    public int? VisibleItemsCountXs { get; set; }

    /// <summary>
    /// Number of visible items in the small breakpoint (from 600px up).
    /// </summary>
    public int? VisibleItemsCountSm { get; set; }

    /// <summary>
    /// Number of visible items in the medium breakpoint (from 960px up).
    /// </summary>
    public int? VisibleItemsCountMd { get; set; }

    /// <summary>
    /// Number of visible items in the large breakpoint (from 1280px up).
    /// </summary>
    public int? VisibleItemsCountLg { get; set; }

    /// <summary>
    /// Number of visible items in the extra large breakpoint (from 1920px up).
    /// </summary>
    public int? VisibleItemsCountXl { get; set; }

    /// <summary>
    /// Number of visible items in the extra extra large breakpoint (from 2560px up).
    /// </summary>
    public int? VisibleItemsCountXxl { get; set; }

    /// <summary>
    /// Navigates the carousel with the wheel of the mouse (or with a two finger scroll on a trackpad).
    /// </summary>
    public bool? Wheel { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCarousel"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCarousel"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCarousel"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCarousel"/>.
    /// </remarks>
    /// <param name="bitCarousel">
    /// The <see cref="BitCarousel"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCarousel bitCarousel)
    {
        if (bitCarousel is null) return;

        UpdateBaseParameters(bitCarousel);

        if (Accent.HasValue && bitCarousel.HasNotBeenSet(nameof(Accent)))
        {
            bitCarousel.Accent = Accent.Value;

            bitCarousel.ClassBuilder.Reset();
        }

        if (AnimationDuration.HasValue && bitCarousel.HasNotBeenSet(nameof(AnimationDuration)))
        {
            bitCarousel.AnimationDuration = AnimationDuration.Value;

            bitCarousel.StyleBuilder.Reset();
        }

        if (AutoPlay.HasValue && bitCarousel.HasNotBeenSet(nameof(AutoPlay)))
        {
            bitCarousel.AutoPlay = AutoPlay.Value;
        }

        if (AutoPlayInterval.HasValue && bitCarousel.HasNotBeenSet(nameof(AutoPlayInterval)))
        {
            bitCarousel.AutoPlayInterval = AutoPlayInterval.Value;
        }

        if (AutoPlayReverse.HasValue && bitCarousel.HasNotBeenSet(nameof(AutoPlayReverse)))
        {
            bitCarousel.AutoPlayReverse = AutoPlayReverse.Value;
        }

        if (Classes is not null && bitCarousel.HasNotBeenSet(nameof(Classes)))
        {
            bitCarousel.Classes = Classes;

            bitCarousel.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitCarousel.HasNotBeenSet(nameof(Color)))
        {
            bitCarousel.Color = Color.Value;

            bitCarousel.ClassBuilder.Reset();
        }

        if (DotAriaLabel.HasValue() && bitCarousel.HasNotBeenSet(nameof(DotAriaLabel)))
        {
            bitCarousel.DotAriaLabel = DotAriaLabel!;
        }

        if (DotsAriaLabel.HasValue() && bitCarousel.HasNotBeenSet(nameof(DotsAriaLabel)))
        {
            bitCarousel.DotsAriaLabel = DotsAriaLabel!;
        }

        if (DotsPosition.HasValue && bitCarousel.HasNotBeenSet(nameof(DotsPosition)))
        {
            bitCarousel.DotsPosition = DotsPosition.Value;

            bitCarousel.ClassBuilder.Reset();
        }

        if (DotTemplate is not null && bitCarousel.HasNotBeenSet(nameof(DotTemplate)))
        {
            bitCarousel.DotTemplate = DotTemplate;
        }

        if (DragThreshold.HasValue && bitCarousel.HasNotBeenSet(nameof(DragThreshold)))
        {
            bitCarousel.DragThreshold = DragThreshold.Value;
        }

        if (Fade.HasValue && bitCarousel.HasNotBeenSet(nameof(Fade)))
        {
            bitCarousel.Fade = Fade.Value;
        }

        if (Gap.HasValue() && bitCarousel.HasNotBeenSet(nameof(Gap)))
        {
            bitCarousel.Gap = Gap;

            bitCarousel.StyleBuilder.Reset();
        }

        if (GoLeftAriaLabel.HasValue() && bitCarousel.HasNotBeenSet(nameof(GoLeftAriaLabel)))
        {
            bitCarousel.GoLeftAriaLabel = GoLeftAriaLabel;
        }

        if (GoLeftIcon is not null && bitCarousel.HasNotBeenSet(nameof(GoLeftIcon)))
        {
            bitCarousel.GoLeftIcon = GoLeftIcon;
        }

        if (GoLeftIconName.HasValue() && bitCarousel.HasNotBeenSet(nameof(GoLeftIconName)))
        {
            bitCarousel.GoLeftIconName = GoLeftIconName;
        }

        if (GoRightAriaLabel.HasValue() && bitCarousel.HasNotBeenSet(nameof(GoRightAriaLabel)))
        {
            bitCarousel.GoRightAriaLabel = GoRightAriaLabel;
        }

        if (GoRightIcon is not null && bitCarousel.HasNotBeenSet(nameof(GoRightIcon)))
        {
            bitCarousel.GoRightIcon = GoRightIcon;
        }

        if (GoRightIconName.HasValue() && bitCarousel.HasNotBeenSet(nameof(GoRightIconName)))
        {
            bitCarousel.GoRightIconName = GoRightIconName;
        }

        if (HideDots.HasValue && bitCarousel.HasNotBeenSet(nameof(HideDots)))
        {
            bitCarousel.HideDots = HideDots.Value;
        }

        if (HideNextPrev.HasValue && bitCarousel.HasNotBeenSet(nameof(HideNextPrev)))
        {
            bitCarousel.HideNextPrev = HideNextPrev.Value;
        }

        if (InfiniteScrolling.HasValue && bitCarousel.HasNotBeenSet(nameof(InfiniteScrolling)))
        {
            bitCarousel.InfiniteScrolling = InfiniteScrolling.Value;
        }

        if (ItemAriaLabelFormat.HasValue() && bitCarousel.HasNotBeenSet(nameof(ItemAriaLabelFormat)))
        {
            bitCarousel.ItemAriaLabelFormat = ItemAriaLabelFormat;
        }

        if (NoDrag.HasValue && bitCarousel.HasNotBeenSet(nameof(NoDrag)))
        {
            bitCarousel.NoDrag = NoDrag.Value;
        }

        if (NoKeyboard.HasValue && bitCarousel.HasNotBeenSet(nameof(NoKeyboard)))
        {
            bitCarousel.NoKeyboard = NoKeyboard.Value;
        }

        if (PauseButtonAriaLabel.HasValue() && bitCarousel.HasNotBeenSet(nameof(PauseButtonAriaLabel)))
        {
            bitCarousel.PauseButtonAriaLabel = PauseButtonAriaLabel!;
        }

        if (PauseIcon is not null && bitCarousel.HasNotBeenSet(nameof(PauseIcon)))
        {
            bitCarousel.PauseIcon = PauseIcon;
        }

        if (PauseIconName.HasValue() && bitCarousel.HasNotBeenSet(nameof(PauseIconName)))
        {
            bitCarousel.PauseIconName = PauseIconName;
        }

        if (PauseOnFocus.HasValue && bitCarousel.HasNotBeenSet(nameof(PauseOnFocus)))
        {
            bitCarousel.PauseOnFocus = PauseOnFocus.Value;
        }

        if (PauseOnHover.HasValue && bitCarousel.HasNotBeenSet(nameof(PauseOnHover)))
        {
            bitCarousel.PauseOnHover = PauseOnHover.Value;
        }

        if (PlayButtonAriaLabel.HasValue() && bitCarousel.HasNotBeenSet(nameof(PlayButtonAriaLabel)))
        {
            bitCarousel.PlayButtonAriaLabel = PlayButtonAriaLabel!;
        }

        if (PlayIcon is not null && bitCarousel.HasNotBeenSet(nameof(PlayIcon)))
        {
            bitCarousel.PlayIcon = PlayIcon;
        }

        if (PlayIconName.HasValue() && bitCarousel.HasNotBeenSet(nameof(PlayIconName)))
        {
            bitCarousel.PlayIconName = PlayIconName;
        }

        if (ResponsiveOptions is not null && bitCarousel.HasNotBeenSet(nameof(ResponsiveOptions)))
        {
            bitCarousel.ResponsiveOptions = ResponsiveOptions;
        }

        if (ScrollItemsCount.HasValue && bitCarousel.HasNotBeenSet(nameof(ScrollItemsCount)))
        {
            bitCarousel.ScrollItemsCount = ScrollItemsCount.Value;
        }

        if (ShowPlayPause.HasValue && bitCarousel.HasNotBeenSet(nameof(ShowPlayPause)))
        {
            bitCarousel.ShowPlayPause = ShowPlayPause.Value;
        }

        if (Size.HasValue && bitCarousel.HasNotBeenSet(nameof(Size)))
        {
            bitCarousel.Size = Size.Value;

            bitCarousel.ClassBuilder.Reset();
        }

        if (StopOnInteraction.HasValue && bitCarousel.HasNotBeenSet(nameof(StopOnInteraction)))
        {
            bitCarousel.StopOnInteraction = StopOnInteraction.Value;
        }

        if (StopOnLastSlide.HasValue && bitCarousel.HasNotBeenSet(nameof(StopOnLastSlide)))
        {
            bitCarousel.StopOnLastSlide = StopOnLastSlide.Value;
        }

        if (Styles is not null && bitCarousel.HasNotBeenSet(nameof(Styles)))
        {
            bitCarousel.Styles = Styles;

            bitCarousel.StyleBuilder.Reset();
        }

        if (Vertical.HasValue && bitCarousel.HasNotBeenSet(nameof(Vertical)))
        {
            bitCarousel.Vertical = Vertical.Value;

            bitCarousel.ClassBuilder.Reset();
        }

        if (VisibleItemsCount.HasValue && bitCarousel.HasNotBeenSet(nameof(VisibleItemsCount)))
        {
            bitCarousel.VisibleItemsCount = VisibleItemsCount.Value;
        }

        if (VisibleItemsCountXs.HasValue && bitCarousel.HasNotBeenSet(nameof(VisibleItemsCountXs)))
        {
            bitCarousel.VisibleItemsCountXs = VisibleItemsCountXs.Value;
        }

        if (VisibleItemsCountSm.HasValue && bitCarousel.HasNotBeenSet(nameof(VisibleItemsCountSm)))
        {
            bitCarousel.VisibleItemsCountSm = VisibleItemsCountSm.Value;
        }

        if (VisibleItemsCountMd.HasValue && bitCarousel.HasNotBeenSet(nameof(VisibleItemsCountMd)))
        {
            bitCarousel.VisibleItemsCountMd = VisibleItemsCountMd.Value;
        }

        if (VisibleItemsCountLg.HasValue && bitCarousel.HasNotBeenSet(nameof(VisibleItemsCountLg)))
        {
            bitCarousel.VisibleItemsCountLg = VisibleItemsCountLg.Value;
        }

        if (VisibleItemsCountXl.HasValue && bitCarousel.HasNotBeenSet(nameof(VisibleItemsCountXl)))
        {
            bitCarousel.VisibleItemsCountXl = VisibleItemsCountXl.Value;
        }

        if (VisibleItemsCountXxl.HasValue && bitCarousel.HasNotBeenSet(nameof(VisibleItemsCountXxl)))
        {
            bitCarousel.VisibleItemsCountXxl = VisibleItemsCountXxl.Value;
        }

        if (Wheel.HasValue && bitCarousel.HasNotBeenSet(nameof(Wheel)))
        {
            bitCarousel.Wheel = Wheel.Value;
        }
    }
}
