namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSwiper"/> component.
/// </summary>
/// <remarks>
/// It carries the look and the behavior shared by the swipers under a <see cref="BitParams"/>, not what belongs
/// to one of them: <see cref="BitSwiper.ChildContent"/>, <see cref="BitSwiper.DefaultItem"/> and
/// <see cref="BitSwiper.OnChange"/> (with <see cref="BitSwiper.OnReachStart"/> and <see cref="BitSwiper.OnReachEnd"/>)
/// are left off, since the items, the place a swiper starts on and what a page does when it moves are the business
/// of the swiper they belong to.
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
    /// The room (any CSS length) kept at both ends of the swiper, which the neighboring items peek into.
    /// </summary>
    public string? Peek { get; set; }

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
    public BitScrollSnapAlign? SnapAlign { get; set; }

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

        if (Accent.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(Accent), Accent.Value, static s => s.Accent, static (s, v) => s.Accent = v);
        }

        if (AnimationDuration.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(AnimationDuration), AnimationDuration.Value, static s => s.AnimationDuration, static (s, v) => s.AnimationDuration = v);
        }

        if (AutoPlay.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(AutoPlay), AutoPlay.Value, static s => s.AutoPlay, static (s, v) => s.AutoPlay = v);
        }

        if (AutoPlayInterval.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(AutoPlayInterval), AutoPlayInterval.Value, static s => s.AutoPlayInterval, static (s, v) => s.AutoPlayInterval = v);
        }

        if (AutoPlayReverse.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(AutoPlayReverse), AutoPlayReverse.Value, static s => s.AutoPlayReverse, static (s, v) => s.AutoPlayReverse = v);
        }

        if (Classes is not null)
        {
            bitSwiper.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Color.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(Color), Color.Value, static s => s.Color, static (s, v) => s.Color = v);
        }

        if (DotAriaLabel.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(DotAriaLabel), DotAriaLabel!, static s => s.DotAriaLabel, static (s, v) => s.DotAriaLabel = v);
        }

        if (DotsAriaLabel.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(DotsAriaLabel), DotsAriaLabel!, static s => s.DotsAriaLabel, static (s, v) => s.DotsAriaLabel = v);
        }

        if (DotTemplate is not null)
        {
            bitSwiper.TakeFromCascade(nameof(DotTemplate), DotTemplate, static s => s.DotTemplate, static (s, v) => s.DotTemplate = v);
        }

        if (DragThreshold.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(DragThreshold), DragThreshold.Value, static s => s.DragThreshold, static (s, v) => s.DragThreshold = v);
        }

        if (Gap.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(Gap), Gap, static s => s.Gap, static (s, v) => s.Gap = v);
        }

        if (HideNextPrev.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(HideNextPrev), HideNextPrev.Value, static s => s.HideNextPrev, static (s, v) => s.HideNextPrev = v);
        }

        if (ItemAriaLabelFormat.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(ItemAriaLabelFormat), ItemAriaLabelFormat, static s => s.ItemAriaLabelFormat, static (s, v) => s.ItemAriaLabelFormat = v);
        }

        if (NextAriaLabel.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(NextAriaLabel), NextAriaLabel, static s => s.NextAriaLabel, static (s, v) => s.NextAriaLabel = v);
        }


        if (NextIcon is not null)
        {
            bitSwiper.TakeFromCascade(nameof(NextIcon), NextIcon, static s => s.NextIcon, static (s, v) => s.NextIcon = v);
        }

        if (NextIconName.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(NextIconName), NextIconName, static s => s.NextIconName, static (s, v) => s.NextIconName = v);
        }

        if (NoDrag.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(NoDrag), NoDrag.Value, static s => s.NoDrag, static (s, v) => s.NoDrag = v);
        }

        if (NoKeyboard.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(NoKeyboard), NoKeyboard.Value, static s => s.NoKeyboard, static (s, v) => s.NoKeyboard = v);
        }

        if (PauseButtonAriaLabel.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(PauseButtonAriaLabel), PauseButtonAriaLabel!, static s => s.PauseButtonAriaLabel, static (s, v) => s.PauseButtonAriaLabel = v);
        }

        if (Peek.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(Peek), Peek, static s => s.Peek, static (s, v) => s.Peek = v);
        }


        if (PauseIcon is not null)
        {
            bitSwiper.TakeFromCascade(nameof(PauseIcon), PauseIcon, static s => s.PauseIcon, static (s, v) => s.PauseIcon = v);
        }

        if (PauseIconName.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(PauseIconName), PauseIconName, static s => s.PauseIconName, static (s, v) => s.PauseIconName = v);
        }

        if (PauseOnFocus.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(PauseOnFocus), PauseOnFocus.Value, static s => s.PauseOnFocus, static (s, v) => s.PauseOnFocus = v);
        }

        if (PauseOnHover.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(PauseOnHover), PauseOnHover.Value, static s => s.PauseOnHover, static (s, v) => s.PauseOnHover = v);
        }

        if (PlayButtonAriaLabel.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(PlayButtonAriaLabel), PlayButtonAriaLabel!, static s => s.PlayButtonAriaLabel, static (s, v) => s.PlayButtonAriaLabel = v);
        }


        if (PlayIcon is not null)
        {
            bitSwiper.TakeFromCascade(nameof(PlayIcon), PlayIcon, static s => s.PlayIcon, static (s, v) => s.PlayIcon = v);
        }

        if (PlayIconName.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(PlayIconName), PlayIconName, static s => s.PlayIconName, static (s, v) => s.PlayIconName = v);
        }

        if (PrevAriaLabel.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(PrevAriaLabel), PrevAriaLabel, static s => s.PrevAriaLabel, static (s, v) => s.PrevAriaLabel = v);
        }


        if (PrevIcon is not null)
        {
            bitSwiper.TakeFromCascade(nameof(PrevIcon), PrevIcon, static s => s.PrevIcon, static (s, v) => s.PrevIcon = v);
        }

        if (PrevIconName.HasValue())
        {
            bitSwiper.TakeFromCascade(nameof(PrevIconName), PrevIconName, static s => s.PrevIconName, static (s, v) => s.PrevIconName = v);
        }

        if (Rewind.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(Rewind), Rewind.Value, static s => s.Rewind, static (s, v) => s.Rewind = v);
        }

        if (ScrollItemsCount.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(ScrollItemsCount), ScrollItemsCount.Value, static s => s.ScrollItemsCount, static (s, v) => s.ScrollItemsCount = v);
        }

        if (ShowDots.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(ShowDots), ShowDots.Value, static s => s.ShowDots, static (s, v) => s.ShowDots = v);
        }

        if (ShowPlayPause.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(ShowPlayPause), ShowPlayPause.Value, static s => s.ShowPlayPause, static (s, v) => s.ShowPlayPause = v);
        }

        if (ShowScrollbar.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(ShowScrollbar), ShowScrollbar.Value, static s => s.ShowScrollbar, static (s, v) => s.ShowScrollbar = v);
        }

        if (Size.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (SnapAlign.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(SnapAlign), SnapAlign.Value, static s => s.SnapAlign, static (s, v) => s.SnapAlign = v);
        }

        if (StopOnInteraction.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(StopOnInteraction), StopOnInteraction.Value, static s => s.StopOnInteraction, static (s, v) => s.StopOnInteraction = v);
        }

        if (StopOnLastSlide.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(StopOnLastSlide), StopOnLastSlide.Value, static s => s.StopOnLastSlide, static (s, v) => s.StopOnLastSlide = v);
        }

        if (Styles is not null)
        {
            bitSwiper.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (Vertical.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(Vertical), Vertical.Value, static s => s.Vertical, static (s, v) => s.Vertical = v);
        }

        if (VisibleItemsCount.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(VisibleItemsCount), VisibleItemsCount.Value, static s => s.VisibleItemsCount, static (s, v) => s.VisibleItemsCount = v);
        }

        if (VisibleItemsCountXs.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(VisibleItemsCountXs), VisibleItemsCountXs.Value, static s => s.VisibleItemsCountXs, static (s, v) => s.VisibleItemsCountXs = v);
        }

        if (VisibleItemsCountSm.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(VisibleItemsCountSm), VisibleItemsCountSm.Value, static s => s.VisibleItemsCountSm, static (s, v) => s.VisibleItemsCountSm = v);
        }

        if (VisibleItemsCountMd.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(VisibleItemsCountMd), VisibleItemsCountMd.Value, static s => s.VisibleItemsCountMd, static (s, v) => s.VisibleItemsCountMd = v);
        }

        if (VisibleItemsCountLg.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(VisibleItemsCountLg), VisibleItemsCountLg.Value, static s => s.VisibleItemsCountLg, static (s, v) => s.VisibleItemsCountLg = v);
        }

        if (VisibleItemsCountXl.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(VisibleItemsCountXl), VisibleItemsCountXl.Value, static s => s.VisibleItemsCountXl, static (s, v) => s.VisibleItemsCountXl = v);
        }

        if (VisibleItemsCountXxl.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(VisibleItemsCountXxl), VisibleItemsCountXxl.Value, static s => s.VisibleItemsCountXxl, static (s, v) => s.VisibleItemsCountXxl = v);
        }

        if (Wheel.HasValue)
        {
            bitSwiper.TakeFromCascade(nameof(Wheel), Wheel.Value, static s => s.Wheel, static (s, v) => s.Wheel = v);
        }
    }
}
