namespace Bit.Bheadless;

/// <summary>The state of a <see cref="BitBhCarousel"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhCarouselContext
{
    internal BitBhCarouselContext(BitBhCarousel owner) => Owner = owner;

    internal BitBhCarousel Owner { get; }

    /// <summary>The first slide in view.</summary>
    public int Index => Owner.CurrentIndex;

    /// <summary>The number of slides.</summary>
    public int Count => Owner.Count;

    /// <summary>The positions an indicator can go to (0 … count − slides per view).</summary>
    public int PageCount => Owner.MaxIndex + 1;

    /// <summary>Whether the carousel is rotating.</summary>
    public bool IsPlaying => Owner.IsPlaying;

    /// <summary>Shows a slide.</summary>
    public Task GoToAsync(int index) => Owner.GoToAsync(index);
}
