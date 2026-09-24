namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSwiper"/> component.
/// </summary>
/// <remarks>
/// It carries the look and the behavior shared by the swipers under a <see cref="BitParams"/>, not what belongs
/// to one of them: <see cref="BitSwiper.ChildContent"/>, <see cref="BitSwiper.DefaultItem"/> and
/// <see cref="BitSwiper.OnChange"/> are left off, since the items, the place a swiper starts on and what a page
/// does when it moves are the business of the swiper they belong to.
/// </remarks>
public class BitSwiperParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSwiper"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSwiper value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSwiper)}";



    public string Name => ParamName;


    /// <summary>
    /// The accent color kind of the swiper, which colors the dot of the current page.
    /// </summary>
    public BitColorKind? Accent { get; set; }

    /// <summary>
    /// The duration (in seconds) of the scrolling animation of the moves the swiper makes itself.
    /// </summary>
    public double? AnimationDuration { get; set; }

    /// <summary>
    /// Enables/disables the auto scrolling of the items.
    /// </summary>
    public bool? AutoPlay { get; set; }

    /// <summary>
    /// The interval of the auto scrolling in milliseconds.
    /// </summary>
    public double? AutoPlayInterval { get; set; }

    /// <summary>
    /// Plays the auto scrolling backwards, from the last item towards the first one.
    /// </summary>
    public bool? AutoPlayReverse { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the swiper.
    /// </summary>
    public BitSwiperClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the swiper, which colors the current dot, the buttons and the focus indicators.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The accessible label of a dot, followed by the number of the page it navigates to.
    /// </summary>
    public string? DotAriaLabel { get; set; }

    /// <summary>
    /// The accessible label of the dots container.
    /// </summary>
    public string? DotsAriaLabel { get; set; }

    /// <summary>
    /// The custom content of a dot, receiving the zero based index of the page the dot navigates to.
    /// </summary>
    public RenderFragment<int>? DotTemplate { get; set; }

    /// <summary>
    /// The distance (in pixels) the pointer has to travel over the swiper before it starts dragging it.
    /// </summary>
    public int? DragThreshold { get; set; }

    /// <summary>
    /// The space between the items of the swiper (any CSS length).
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Hides the next/prev buttons of the swiper.
    /// </summary>
    public bool? HideNextPrev { get; set; }

    /// <summary>
    /// The accessible label of an item without one of its own, as a composite format string ({0} the position, {1} the count).
    /// </summary>
    public string? ItemAriaLabelFormat { get; set; }

    /// <summary>
    /// The accessible label of the next button.
    /// </summary>
    public string? NextAriaLabel { get; set; }

    /// <summary>
    /// The icon of the next button, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? NextIcon { get; set; }

    /// <summary>
    /// The name of the icon of the next button, from the built-in Fluent UI icons.
    /// </summary>
    public string? NextIconName { get; set; }

    /// <summary>
    /// Disables dragging the swiper with the mouse.
    /// </summary>
    public bool? NoDrag { get; set; }

    /// <summary>
    /// Removes the swiper from the tab sequence and turns off its keyboard navigation.
    /// </summary>
    public bool? NoKeyboard { get; set; }

    /// <summary>
    /// The accessible label of the play/pause button while the auto scrolling is running.
    /// </summary>
    public string? PauseButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the play/pause button while the auto scrolling is running, for external icon libraries.
    /// </summary>
    public BitIconInfo? PauseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the play/pause button while the auto scrolling is running.
    /// </summary>
    public string? PauseIconName { get; set; }

    /// <summary>
    /// Pauses the auto scrolling while the keyboard focus is inside the swiper.
    /// </summary>
    public bool? PauseOnFocus { get; set; }

    /// <summary>
    /// Pauses the auto scrolling while the pointer is over the swiper.
    /// </summary>
    public bool? PauseOnHover { get; set; }

    /// <summary>
    /// The accessible label of the play/pause button while the auto scrolling is paused.
    /// </summary>
    public string? PlayButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the play/pause button while the auto scrolling is paused, for external icon libraries.
    /// </summary>
    public BitIconInfo? PlayIcon { get; set; }

    /// <summary>
    /// The name of the icon of the play/pause button while the auto scrolling is paused.
    /// </summary>
    public string? PlayIconName { get; set; }

    /// <summary>
    /// The accessible label of the previous button.
    /// </summary>
    public string? PrevAriaLabel { get; set; }

    /// <summary>
    /// The icon of the previous button, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? PrevIcon { get; set; }

    /// <summary>
    /// The name of the icon of the previous button, from the built-in Fluent UI icons.
    /// </summary>
    public string? PrevIconName { get; set; }

    /// <summary>
    /// Wraps the manual navigation around: moving on from the end goes back to the start, and the other way around.
    /// </summary>
    public bool? Rewind { get; set; }

    /// <summary>
    /// Number of items that is going to be changed on navigation.
    /// </summary>
    public int? ScrollItemsCount { get; set; }

    /// <summary>
    /// Renders the navigation dots below the items of the swiper.
    /// </summary>
    public bool? ShowDots { get; set; }

    /// <summary>
    /// Renders a play/pause button next to the dots while the auto scrolling is enabled.
    /// </summary>
    public bool? ShowPlayPause { get; set; }

    /// <summary>
    /// Leaves the scrollbar of the swiper visible.
    /// </summary>
    public bool? ShowScrollbar { get; set; }

    /// <summary>
    /// The size of the dots and of the next/prev buttons of the swiper.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Settles the swiper on an item, at its start, its center or its end, instead of wherever the scrolling ran out.
    /// </summary>
    public BitSwiperSnap? Snap { get; set; }

    /// <summary>
    /// Stops the auto scrolling as soon as the swiper is navigated through one of its own controls.
    /// </summary>
    public bool? StopOnInteraction { get; set; }

    /// <summary>
    /// Stops the auto scrolling at the end of the swiper instead of rewinding to its start.
    /// </summary>
    public bool? StopOnLastSlide { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the swiper.
    /// </summary>
    public BitSwiperClassStyles? Styles { get; set; }

    /// <summary>
    /// Stacks the items vertically, so the swiper scrolls up and down.
    /// </summary>
    public bool? Vertical { get; set; }

    /// <summary>
    /// Number of items that is visible in the swiper, which sizes the items accordingly.
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
    /// Navigates the swiper with the wheel of the mouse.
    /// </summary>
    public bool? Wheel { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSwiper"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSwiper"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSwiper"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitSwiper"/>.
    /// </remarks>
    /// <param name="bitSwiper">
    /// The <see cref="BitSwiper"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSwiper bitSwiper)
    {
        if (bitSwiper is null) return;

        UpdateBaseParameters(bitSwiper);

        if (Accent.HasValue && bitSwiper.HasNotBeenSet(nameof(Accent)))
        {
            bitSwiper.Accent = Accent.Value;

            bitSwiper.ClassBuilder.Reset();
        }

        if (AnimationDuration.HasValue && bitSwiper.HasNotBeenSet(nameof(AnimationDuration)))
        {
            bitSwiper.AnimationDuration = AnimationDuration.Value;
        }

        if (AutoPlay.HasValue && bitSwiper.HasNotBeenSet(nameof(AutoPlay)))
        {
            bitSwiper.AutoPlay = AutoPlay.Value;
        }

        if (AutoPlayInterval.HasValue && bitSwiper.HasNotBeenSet(nameof(AutoPlayInterval)))
        {
            bitSwiper.AutoPlayInterval = AutoPlayInterval.Value;
        }

        if (AutoPlayReverse.HasValue && bitSwiper.HasNotBeenSet(nameof(AutoPlayReverse)))
        {
            bitSwiper.AutoPlayReverse = AutoPlayReverse.Value;
        }

        if (Classes is not null && bitSwiper.HasNotBeenSet(nameof(Classes)))
        {
            bitSwiper.Classes = Classes;

            bitSwiper.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitSwiper.HasNotBeenSet(nameof(Color)))
        {
            bitSwiper.Color = Color.Value;

            bitSwiper.ClassBuilder.Reset();
        }

        if (DotAriaLabel.HasValue() && bitSwiper.HasNotBeenSet(nameof(DotAriaLabel)))
        {
            bitSwiper.DotAriaLabel = DotAriaLabel;
        }

        if (DotsAriaLabel.HasValue() && bitSwiper.HasNotBeenSet(nameof(DotsAriaLabel)))
        {
            bitSwiper.DotsAriaLabel = DotsAriaLabel;
        }

        if (DotTemplate is not null && bitSwiper.HasNotBeenSet(nameof(DotTemplate)))
        {
            bitSwiper.DotTemplate = DotTemplate;
        }

        if (DragThreshold.HasValue && bitSwiper.HasNotBeenSet(nameof(DragThreshold)))
        {
            bitSwiper.DragThreshold = DragThreshold.Value;
        }

        if (Gap.HasValue() && bitSwiper.HasNotBeenSet(nameof(Gap)))
        {
            bitSwiper.Gap = Gap;

            bitSwiper.StyleBuilder.Reset();
        }

        if (HideNextPrev.HasValue && bitSwiper.HasNotBeenSet(nameof(HideNextPrev)))
        {
            bitSwiper.HideNextPrev = HideNextPrev.Value;
        }

        if (ItemAriaLabelFormat.HasValue() && bitSwiper.HasNotBeenSet(nameof(ItemAriaLabelFormat)))
        {
            bitSwiper.ItemAriaLabelFormat = ItemAriaLabelFormat;
        }

        if (NextAriaLabel.HasValue() && bitSwiper.HasNotBeenSet(nameof(NextAriaLabel)))
        {
            bitSwiper.NextAriaLabel = NextAriaLabel;
        }

        if (NextIcon is not null && bitSwiper.HasNotBeenSet(nameof(NextIcon)))
        {
            bitSwiper.NextIcon = NextIcon;
        }

        if (NextIconName.HasValue() && bitSwiper.HasNotBeenSet(nameof(NextIconName)))
        {
            bitSwiper.NextIconName = NextIconName;
        }

        if (NoDrag.HasValue && bitSwiper.HasNotBeenSet(nameof(NoDrag)))
        {
            bitSwiper.NoDrag = NoDrag.Value;

            bitSwiper.ClassBuilder.Reset();
        }

        if (NoKeyboard.HasValue && bitSwiper.HasNotBeenSet(nameof(NoKeyboard)))
        {
            bitSwiper.NoKeyboard = NoKeyboard.Value;
        }

        if (PauseButtonAriaLabel.HasValue() && bitSwiper.HasNotBeenSet(nameof(PauseButtonAriaLabel)))
        {
            bitSwiper.PauseButtonAriaLabel = PauseButtonAriaLabel;
        }

        if (PauseIcon is not null && bitSwiper.HasNotBeenSet(nameof(PauseIcon)))
        {
            bitSwiper.PauseIcon = PauseIcon;
        }

        if (PauseIconName.HasValue() && bitSwiper.HasNotBeenSet(nameof(PauseIconName)))
        {
            bitSwiper.PauseIconName = PauseIconName;
        }

        if (PauseOnFocus.HasValue && bitSwiper.HasNotBeenSet(nameof(PauseOnFocus)))
        {
            bitSwiper.PauseOnFocus = PauseOnFocus.Value;
        }

        if (PauseOnHover.HasValue && bitSwiper.HasNotBeenSet(nameof(PauseOnHover)))
        {
            bitSwiper.PauseOnHover = PauseOnHover.Value;
        }

        if (PlayButtonAriaLabel.HasValue() && bitSwiper.HasNotBeenSet(nameof(PlayButtonAriaLabel)))
        {
            bitSwiper.PlayButtonAriaLabel = PlayButtonAriaLabel;
        }

        if (PlayIcon is not null && bitSwiper.HasNotBeenSet(nameof(PlayIcon)))
        {
            bitSwiper.PlayIcon = PlayIcon;
        }

        if (PlayIconName.HasValue() && bitSwiper.HasNotBeenSet(nameof(PlayIconName)))
        {
            bitSwiper.PlayIconName = PlayIconName;
        }

        if (PrevAriaLabel.HasValue() && bitSwiper.HasNotBeenSet(nameof(PrevAriaLabel)))
        {
            bitSwiper.PrevAriaLabel = PrevAriaLabel;
        }

        if (PrevIcon is not null && bitSwiper.HasNotBeenSet(nameof(PrevIcon)))
        {
            bitSwiper.PrevIcon = PrevIcon;
        }

        if (PrevIconName.HasValue() && bitSwiper.HasNotBeenSet(nameof(PrevIconName)))
        {
            bitSwiper.PrevIconName = PrevIconName;
        }

        if (Rewind.HasValue && bitSwiper.HasNotBeenSet(nameof(Rewind)))
        {
            bitSwiper.Rewind = Rewind.Value;
        }

        if (ScrollItemsCount.HasValue && bitSwiper.HasNotBeenSet(nameof(ScrollItemsCount)))
        {
            bitSwiper.ScrollItemsCount = ScrollItemsCount.Value;
        }

        if (ShowDots.HasValue && bitSwiper.HasNotBeenSet(nameof(ShowDots)))
        {
            bitSwiper.ShowDots = ShowDots.Value;
        }

        if (ShowPlayPause.HasValue && bitSwiper.HasNotBeenSet(nameof(ShowPlayPause)))
        {
            bitSwiper.ShowPlayPause = ShowPlayPause.Value;
        }

        if (ShowScrollbar.HasValue && bitSwiper.HasNotBeenSet(nameof(ShowScrollbar)))
        {
            bitSwiper.ShowScrollbar = ShowScrollbar.Value;

            bitSwiper.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitSwiper.HasNotBeenSet(nameof(Size)))
        {
            bitSwiper.Size = Size.Value;

            bitSwiper.ClassBuilder.Reset();
        }

        if (Snap.HasValue && bitSwiper.HasNotBeenSet(nameof(Snap)))
        {
            bitSwiper.Snap = Snap.Value;

            bitSwiper.ClassBuilder.Reset();
        }

        if (StopOnInteraction.HasValue && bitSwiper.HasNotBeenSet(nameof(StopOnInteraction)))
        {
            bitSwiper.StopOnInteraction = StopOnInteraction.Value;
        }

        if (StopOnLastSlide.HasValue && bitSwiper.HasNotBeenSet(nameof(StopOnLastSlide)))
        {
            bitSwiper.StopOnLastSlide = StopOnLastSlide.Value;
        }

        if (Styles is not null && bitSwiper.HasNotBeenSet(nameof(Styles)))
        {
            bitSwiper.Styles = Styles;

            bitSwiper.StyleBuilder.Reset();
        }

        if (Vertical.HasValue && bitSwiper.HasNotBeenSet(nameof(Vertical)))
        {
            bitSwiper.Vertical = Vertical.Value;

            bitSwiper.ClassBuilder.Reset();
        }

        if (VisibleItemsCount.HasValue && bitSwiper.HasNotBeenSet(nameof(VisibleItemsCount)))
        {
            bitSwiper.VisibleItemsCount = VisibleItemsCount.Value;

            bitSwiper.StyleBuilder.Reset();
        }

        if (VisibleItemsCountXs.HasValue && bitSwiper.HasNotBeenSet(nameof(VisibleItemsCountXs)))
        {
            bitSwiper.VisibleItemsCountXs = VisibleItemsCountXs.Value;

            bitSwiper.StyleBuilder.Reset();
        }

        if (VisibleItemsCountSm.HasValue && bitSwiper.HasNotBeenSet(nameof(VisibleItemsCountSm)))
        {
            bitSwiper.VisibleItemsCountSm = VisibleItemsCountSm.Value;

            bitSwiper.StyleBuilder.Reset();
        }

        if (VisibleItemsCountMd.HasValue && bitSwiper.HasNotBeenSet(nameof(VisibleItemsCountMd)))
        {
            bitSwiper.VisibleItemsCountMd = VisibleItemsCountMd.Value;

            bitSwiper.StyleBuilder.Reset();
        }

        if (VisibleItemsCountLg.HasValue && bitSwiper.HasNotBeenSet(nameof(VisibleItemsCountLg)))
        {
            bitSwiper.VisibleItemsCountLg = VisibleItemsCountLg.Value;

            bitSwiper.StyleBuilder.Reset();
        }

        if (VisibleItemsCountXl.HasValue && bitSwiper.HasNotBeenSet(nameof(VisibleItemsCountXl)))
        {
            bitSwiper.VisibleItemsCountXl = VisibleItemsCountXl.Value;

            bitSwiper.StyleBuilder.Reset();
        }

        if (VisibleItemsCountXxl.HasValue && bitSwiper.HasNotBeenSet(nameof(VisibleItemsCountXxl)))
        {
            bitSwiper.VisibleItemsCountXxl = VisibleItemsCountXxl.Value;

            bitSwiper.StyleBuilder.Reset();
        }

        if (Wheel.HasValue && bitSwiper.HasNotBeenSet(nameof(Wheel)))
        {
            bitSwiper.Wheel = Wheel.Value;
        }
    }
}
