namespace Bit.BlazorUI;

/// <summary>
/// A single slide of a <see cref="BitCarousel"/>.
/// </summary>
/// <remarks>
/// The item is laid out and moved by the carousel that holds it, so its size, its position and its
/// transition are all assigned from there. Everything that describes the slide itself (its content,
/// its classes and styles, and the label assistive technologies read for it) belongs here.
/// </remarks>
public partial class BitCarouselItem : BitComponentBase
{
    [CascadingParameter] protected BitCarousel? Carousel { get; set; }



    /// <summary>
    /// The content of the carousel item (slide).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }



    /// <summary>
    /// The zero based position of this item among the items of its carousel.
    /// </summary>
    /// <remarks>
    /// It is assigned by the carousel when the item registers itself, and kept up to date when
    /// items are added to or removed from the carousel at runtime.
    /// </remarks>
    public int Index { get; internal set; }



    private string internalStyle = string.Empty;
    internal string InternalStyle
    {
        get => internalStyle;
        set
        {
            if (internalStyle == value) return;

            internalStyle = value;
            StyleBuilder.Reset();
        }
    }

    private string internalTransformStyle = string.Empty;
    internal string InternalTransformStyle
    {
        get => internalTransformStyle;
        set
        {
            if (internalTransformStyle == value) return;

            internalTransformStyle = value;
            StyleBuilder.Reset();
        }
    }

    private string internalTransitionStyle = string.Empty;
    internal string InternalTransitionStyle
    {
        get => internalTransitionStyle;
        set
        {
            if (internalTransitionStyle == value) return;

            internalTransitionStyle = value;
            StyleBuilder.Reset();
        }
    }

    // The fade effect cross-fades the slides in place instead of translating them, so the opacity
    // (and the stacking order that keeps the incoming slide on top) is held apart from the transform.
    private string internalFadeStyle = string.Empty;
    internal string InternalFadeStyle
    {
        get => internalFadeStyle;
        set
        {
            if (internalFadeStyle == value) return;

            internalFadeStyle = value;
            StyleBuilder.Reset();
        }
    }

    // Whether the slide is one of the ones currently on screen. It drives the inert/aria-hidden
    // state, so the slides that are scrolled out of the view are not reachable with the keyboard
    // and are not read out by screen readers.
    private bool internalIsCurrent = true;
    internal bool InternalIsCurrent
    {
        get => internalIsCurrent;
        set
        {
            if (internalIsCurrent == value) return;

            internalIsCurrent = value;
            StateHasChanged();
        }
    }

    protected override string RootElementClass => "bit-crsi";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Carousel?.Classes?.Item);
    }

    protected override void RegisterCssStyles()
    {
        // These are registered before the Style parameter of the base class, so an explicit style
        // assigned to a slide still wins over the layout the carousel computes for it.
        StyleBuilder.Register(() => InternalStyle);
        StyleBuilder.Register(() => InternalTransformStyle);
        StyleBuilder.Register(() => InternalTransitionStyle);
        StyleBuilder.Register(() => InternalFadeStyle);
        StyleBuilder.Register(() => Carousel?.Styles?.Item);
    }

    protected override Task OnInitializedAsync()
    {
        Carousel?.RegisterItem(this);

        return base.OnInitializedAsync();
    }



    // The accessible name of a slide falls back to its position, which is what the carousel pattern
    // of the ARIA authoring practices asks for when a slide has nothing better to be called.
    private string? GetAriaLabel()
    {
        if (AriaLabel.HasValue()) return AriaLabel;

        var count = Carousel?.ItemsCount ?? 0;

        return count > 0 ? $"{Index + 1} of {count}" : null;
    }



    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return base.DisposeAsync(disposing);

        Carousel?.UnregisterItem(this);

        return base.DisposeAsync(disposing);
    }
}
