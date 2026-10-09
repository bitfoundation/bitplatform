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
    /// Where the dots (and the play/pause button) are placed around the slides: Bottom (the default), Top,
    /// Start or End, with Start and End following the reading direction. Every other value renders the default.
    /// </summary>
    public BitPlacement? DotsPlacement { get; set; }

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

        if (Accent.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(Accent), Accent.Value, static c => c.Accent, static (c, v) => c.Accent = v);
        }

        if (AnimationDuration.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(AnimationDuration), AnimationDuration.Value, static c => c.AnimationDuration, static (c, v) => c.AnimationDuration = v);
        }

        if (AutoPlay.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(AutoPlay), AutoPlay.Value, static c => c.AutoPlay, static (c, v) => c.AutoPlay = v);
        }

        if (AutoPlayInterval.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(AutoPlayInterval), AutoPlayInterval.Value, static c => c.AutoPlayInterval, static (c, v) => c.AutoPlayInterval = v);
        }

        if (AutoPlayReverse.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(AutoPlayReverse), AutoPlayReverse.Value, static c => c.AutoPlayReverse, static (c, v) => c.AutoPlayReverse = v);
        }

        if (Classes is not null)
        {
            bitCarousel.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Color.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(Color), Color.Value, static c => c.Color, static (c, v) => c.Color = v);
        }

        if (DotAriaLabel.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(DotAriaLabel), DotAriaLabel!, static c => c.DotAriaLabel, static (c, v) => c.DotAriaLabel = v);
        }

        if (DotsAriaLabel.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(DotsAriaLabel), DotsAriaLabel!, static c => c.DotsAriaLabel, static (c, v) => c.DotsAriaLabel = v);
        }

        if (DotsPlacement.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(DotsPlacement), DotsPlacement.Value, static c => c.DotsPlacement, static (c, v) => c.DotsPlacement = v);
        }

        if (DotTemplate is not null)
        {
            bitCarousel.TakeFromCascade(nameof(DotTemplate), DotTemplate, static c => c.DotTemplate, static (c, v) => c.DotTemplate = v);
        }

        if (DragThreshold.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(DragThreshold), DragThreshold.Value, static c => c.DragThreshold, static (c, v) => c.DragThreshold = v);
        }

        if (Fade.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(Fade), Fade.Value, static c => c.Fade, static (c, v) => c.Fade = v);
        }

        if (Gap.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(Gap), Gap, static c => c.Gap, static (c, v) => c.Gap = v);
        }

        if (GoLeftAriaLabel.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(GoLeftAriaLabel), GoLeftAriaLabel, static c => c.GoLeftAriaLabel, static (c, v) => c.GoLeftAriaLabel = v);
        }


        if (GoLeftIcon is not null)
        {
            bitCarousel.TakeFromCascade(nameof(GoLeftIcon), GoLeftIcon, static c => c.GoLeftIcon, static (c, v) => c.GoLeftIcon = v);
        }

        if (GoLeftIconName.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(GoLeftIconName), GoLeftIconName, static c => c.GoLeftIconName, static (c, v) => c.GoLeftIconName = v);
        }

        if (GoRightAriaLabel.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(GoRightAriaLabel), GoRightAriaLabel, static c => c.GoRightAriaLabel, static (c, v) => c.GoRightAriaLabel = v);
        }


        if (GoRightIcon is not null)
        {
            bitCarousel.TakeFromCascade(nameof(GoRightIcon), GoRightIcon, static c => c.GoRightIcon, static (c, v) => c.GoRightIcon = v);
        }

        if (GoRightIconName.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(GoRightIconName), GoRightIconName, static c => c.GoRightIconName, static (c, v) => c.GoRightIconName = v);
        }

        if (HideDots.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(HideDots), HideDots.Value, static c => c.HideDots, static (c, v) => c.HideDots = v);
        }

        if (HideNextPrev.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(HideNextPrev), HideNextPrev.Value, static c => c.HideNextPrev, static (c, v) => c.HideNextPrev = v);
        }

        if (InfiniteScrolling.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(InfiniteScrolling), InfiniteScrolling.Value, static c => c.InfiniteScrolling, static (c, v) => c.InfiniteScrolling = v);
        }

        if (ItemAriaLabelFormat.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(ItemAriaLabelFormat), ItemAriaLabelFormat, static c => c.ItemAriaLabelFormat, static (c, v) => c.ItemAriaLabelFormat = v);
        }

        if (NoDrag.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(NoDrag), NoDrag.Value, static c => c.NoDrag, static (c, v) => c.NoDrag = v);
        }

        if (NoKeyboard.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(NoKeyboard), NoKeyboard.Value, static c => c.NoKeyboard, static (c, v) => c.NoKeyboard = v);
        }

        if (PauseButtonAriaLabel.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(PauseButtonAriaLabel), PauseButtonAriaLabel!, static c => c.PauseButtonAriaLabel, static (c, v) => c.PauseButtonAriaLabel = v);
        }


        if (PauseIcon is not null)
        {
            bitCarousel.TakeFromCascade(nameof(PauseIcon), PauseIcon, static c => c.PauseIcon, static (c, v) => c.PauseIcon = v);
        }

        if (PauseIconName.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(PauseIconName), PauseIconName, static c => c.PauseIconName, static (c, v) => c.PauseIconName = v);
        }

        if (PauseOnFocus.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(PauseOnFocus), PauseOnFocus.Value, static c => c.PauseOnFocus, static (c, v) => c.PauseOnFocus = v);
        }

        if (PauseOnHover.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(PauseOnHover), PauseOnHover.Value, static c => c.PauseOnHover, static (c, v) => c.PauseOnHover = v);
        }

        if (PlayButtonAriaLabel.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(PlayButtonAriaLabel), PlayButtonAriaLabel!, static c => c.PlayButtonAriaLabel, static (c, v) => c.PlayButtonAriaLabel = v);
        }


        if (PlayIcon is not null)
        {
            bitCarousel.TakeFromCascade(nameof(PlayIcon), PlayIcon, static c => c.PlayIcon, static (c, v) => c.PlayIcon = v);
        }

        if (PlayIconName.HasValue())
        {
            bitCarousel.TakeFromCascade(nameof(PlayIconName), PlayIconName, static c => c.PlayIconName, static (c, v) => c.PlayIconName = v);
        }

        if (ResponsiveOptions is not null)
        {
            bitCarousel.TakeFromCascade(nameof(ResponsiveOptions), ResponsiveOptions, static c => c.ResponsiveOptions, static (c, v) => c.ResponsiveOptions = v);
        }

        if (ScrollItemsCount.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(ScrollItemsCount), ScrollItemsCount.Value, static c => c.ScrollItemsCount, static (c, v) => c.ScrollItemsCount = v);
        }

        if (ShowPlayPause.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(ShowPlayPause), ShowPlayPause.Value, static c => c.ShowPlayPause, static (c, v) => c.ShowPlayPause = v);
        }

        if (Size.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (StopOnInteraction.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(StopOnInteraction), StopOnInteraction.Value, static c => c.StopOnInteraction, static (c, v) => c.StopOnInteraction = v);
        }

        if (StopOnLastSlide.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(StopOnLastSlide), StopOnLastSlide.Value, static c => c.StopOnLastSlide, static (c, v) => c.StopOnLastSlide = v);
        }

        if (Styles is not null)
        {
            bitCarousel.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (Vertical.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(Vertical), Vertical.Value, static c => c.Vertical, static (c, v) => c.Vertical = v);
        }

        if (VisibleItemsCount.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(VisibleItemsCount), VisibleItemsCount.Value, static c => c.VisibleItemsCount, static (c, v) => c.VisibleItemsCount = v);
        }

        if (VisibleItemsCountXs.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(VisibleItemsCountXs), VisibleItemsCountXs.Value, static c => c.VisibleItemsCountXs, static (c, v) => c.VisibleItemsCountXs = v);
        }

        if (VisibleItemsCountSm.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(VisibleItemsCountSm), VisibleItemsCountSm.Value, static c => c.VisibleItemsCountSm, static (c, v) => c.VisibleItemsCountSm = v);
        }

        if (VisibleItemsCountMd.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(VisibleItemsCountMd), VisibleItemsCountMd.Value, static c => c.VisibleItemsCountMd, static (c, v) => c.VisibleItemsCountMd = v);
        }

        if (VisibleItemsCountLg.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(VisibleItemsCountLg), VisibleItemsCountLg.Value, static c => c.VisibleItemsCountLg, static (c, v) => c.VisibleItemsCountLg = v);
        }

        if (VisibleItemsCountXl.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(VisibleItemsCountXl), VisibleItemsCountXl.Value, static c => c.VisibleItemsCountXl, static (c, v) => c.VisibleItemsCountXl = v);
        }

        if (VisibleItemsCountXxl.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(VisibleItemsCountXxl), VisibleItemsCountXxl.Value, static c => c.VisibleItemsCountXxl, static (c, v) => c.VisibleItemsCountXxl = v);
        }

        if (Wheel.HasValue)
        {
            bitCarousel.TakeFromCascade(nameof(Wheel), Wheel.Value, static c => c.Wheel, static (c, v) => c.Wheel = v);
        }
    }
}
