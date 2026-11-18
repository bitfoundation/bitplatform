namespace Bit.Bheadless;

/// <summary>Shows the previous slides of a <see cref="BitBhCarousel"/> (named "Previous slide"; disabled at the start unless it loops).</summary>
public class BitBhCarouselPrevious : BitBhCarouselButtonBase
{
    protected override bool IsDisabled => Owner is not { CanGoPrevious: true };

    protected override Task ActivateAsync(BitBhCarousel owner) => owner.PreviousAsync();

    protected override string Label => Strings.PreviousSlide;
}
