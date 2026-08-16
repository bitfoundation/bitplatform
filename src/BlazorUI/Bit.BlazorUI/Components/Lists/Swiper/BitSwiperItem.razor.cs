using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// A single item (slide) of a <see cref="BitSwiper"/>.
/// </summary>
/// <remarks>
/// The item keeps whatever size it is given, unless the swiper was told how many items to show at a time
/// (<see cref="BitSwiper.VisibleItemsCount"/>), in which case it takes a fraction of the swiper. Unlike
/// the slides of a <see cref="BitCarousel"/>, the items of a swiper are never moved one by one: they sit
/// in a scrolling row and the whole row is scrolled, so nothing about their position is assigned here.
/// </remarks>
public partial class BitSwiperItem : BitComponentBase
{
    [CascadingParameter] protected BitSwiper? Swiper { get; set; }



    /// <summary>
    /// The content of the swiper item.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }



    /// <summary>
    /// The zero based position of this item among the items of its swiper.
    /// </summary>
    /// <remarks>
    /// It is assigned by the swiper when the item registers itself, and kept up to date when items are
    /// added to or removed from the swiper at runtime.
    /// </remarks>
    public int Index
    {
        get => index;
        internal set
        {
            if (index == value) return;

            index = value;
            ClassBuilder.Reset();
        }
    }
    private int index;



    // Whether the swiper is currently standing on this item. It is what the current item class and style
    // of the swiper hang on. Unlike a carousel, a swiper never hides the items it is not standing on:
    // they are all on screen (or one scroll away from it), so they stay readable and reachable.
    private bool internalIsCurrent;
    internal bool InternalIsCurrent
    {
        get => internalIsCurrent;
        set
        {
            if (internalIsCurrent == value) return;

            internalIsCurrent = value;
            ClassBuilder.Reset();
            StyleBuilder.Reset();
            StateHasChanged();
        }
    }

    // The accessible name an item falls back to counts the items around it, so the swiper asks its items
    // to render again when it gains or loses one of them.
    internal void Refresh()
    {
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-swpi";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Swiper?.Classes?.Item);

        ClassBuilder.Register(() => InternalIsCurrent
                                    ? $"bit-swpi-cur {Swiper?.Classes?.CurrentItem}".Trim()
                                    : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        // These are registered before the Style parameter of the base class, so an explicit style assigned
        // to an item still wins over the one the swiper gives it.
        StyleBuilder.Register(() => Swiper?.Styles?.Item);
        StyleBuilder.Register(() => InternalIsCurrent ? Swiper?.Styles?.CurrentItem : null);
    }

    protected override Task OnInitializedAsync()
    {
        Swiper?.RegisterItem(this);

        return base.OnInitializedAsync();
    }



    // The accessible name of an item falls back to its position, which is what the carousel pattern of the
    // ARIA authoring practices asks for when a slide has nothing better to be called.
    private string? GetAriaLabel()
    {
        if (AriaLabel.HasValue()) return AriaLabel;

        var count = Swiper?.ItemsCount ?? 0;

        if (count < 1) return null;

        var format = Swiper?.ItemAriaLabelFormat;

        return string.Format(CultureInfo.CurrentCulture, format.HasValue() ? format! : "{0} of {1}", Index + 1, count);
    }



    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return base.DisposeAsync(disposing);

        Swiper?.UnregisterItem(this);

        return base.DisposeAsync(disposing);
    }
}
