namespace Bit.Bheadless;

/// <summary>Shows the next slides of a <see cref="BitBhCarousel"/> (named "Next slide"; disabled at the end unless it loops).</summary>
public class BitBhCarouselNext : BitBhCarouselButtonBase
{
    protected override bool IsDisabled => Owner is not { CanGoNext: true };

    protected override Task ActivateAsync(BitBhCarousel owner) => owner.NextAsync();

    protected override string Label => Strings.NextSlide;
}
