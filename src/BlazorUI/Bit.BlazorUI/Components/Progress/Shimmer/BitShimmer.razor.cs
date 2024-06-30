namespace Bit.BlazorUI;

public partial class BitShimmer : BitComponentBase
{
    private BitShimmerShape shape = BitShimmerShape.Line;
    private BitShimmerAnimation animation = BitShimmerAnimation.Wave;



    /// <summary>
    /// The animation of the shimmer.
    /// </summary>
    [Parameter]
    public BitShimmerAnimation Animation
    {
        get => animation;
        set
        {
            if (animation == value) return;

            animation = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Child content of component, the content that the shimmer will apply to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitShimmer.
    /// </summary>
    [Parameter] public BitShimmerClassStyles? Classes { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// The shimmer height value.
    /// </summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>
    /// Controls when the shimmer is swapped with actual data through an animated transition.
    /// </summary>
    [Parameter] public bool IsDataLoaded { get; set; }

    /// <summary>
    /// The shape of the shimmer.
    /// </summary>
    [Parameter]
    public BitShimmerShape Shape
    {
        get => shape;
        set
        {
            if (shape == value) return;

            shape = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The template of the shimmer.
    /// </summary>
    [Parameter] public RenderFragment? ShimmerTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitShimmer.
    /// </summary>
    [Parameter] public BitShimmerClassStyles? Styles { get; set; }

    /// <summary>
    /// The shimmer width value.
    /// </summary>
    [Parameter] public string? Width { get; set; }


    protected override string RootElementClass => "bit-smr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);
        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);
    }


    private string GetShapesClass() => Shape switch
    {
        BitShimmerShape.Line => "bit-smr-lin",
        BitShimmerShape.Circle => "bit-smr-crl",
        BitShimmerShape.Rectangle => "bit-smr-rct",
        _ => "bit-smr-lin"
    };

    private string GetAnimationClass() => Animation switch
    {
        BitShimmerAnimation.Wave => "bit-smr-wav",
        BitShimmerAnimation.Pulse => "bit-smr-pul",
        _ => "bit-smr-wav"
    };
}
