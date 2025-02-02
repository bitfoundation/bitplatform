namespace Bit.BlazorUI;

/// <summary>
/// Shimmer is a temporary animation placeholder for when a service call takes time to return data and you don't want to block rendering the rest of the UI.
/// </summary>
public partial class BitShimmer : BitComponentBase
{
    /// <summary>
    /// The animation delay value in ms.
    /// </summary>
    [Parameter]
    public int? AnimationDelay { get; set; }

    /// <summary>
    /// The animation duration value in ms.
    /// </summary>
    [Parameter]
    public int? AnimationDuration { get; set; }

    /// <summary>
    /// The color of the animated part of the shimmer.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? BackgroundColor { get; set; }

    /// <summary>
    /// Changes the animation type of the shimmer to pulse.
    /// </summary>
    [Parameter] public bool Pulse { get; set; }

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
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Controls when the shimmer is swapped with actual data through an animated transition.
    /// </summary>
    [Parameter] public bool IsDataLoaded { get; set; }

    /// <summary>
    /// Changes the shape of the shimmer to circle.
    /// </summary>
    [Parameter] public bool Circle { get; set; }

    /// <summary>
    /// The template of the shimmer.
    /// </summary>
    [Parameter] public RenderFragment? ShimmerTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitShimmer.
    /// </summary>
    [Parameter] public BitShimmerClassStyles? Styles { get; set; }
    /// <summary>
    /// The color of the animated part of the shimmer.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? WaveColor { get; set; }

    /// <summary>
    /// The shimmer width value.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    protected override string RootElementClass => "bit-smr";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);
        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => WaveColor switch
        {
            BitColor.Primary => "bit-smr-pri",
            BitColor.Secondary => "bit-smr-sec",
            BitColor.Tertiary => "bit-smr-ter",
            BitColor.Info => "bit-smr-inf",
            BitColor.Success => "bit-smr-suc",
            BitColor.Warning => "bit-smr-wrn",
            BitColor.SevereWarning => "bit-smr-swr",
            BitColor.Error => "bit-smr-err",
            BitColor.PrimaryBackground => "bit-smr-pbg",
            BitColor.SecondaryBackground => "bit-smr-sbg",
            BitColor.TertiaryBackground => "bit-smr-tbg",
            BitColor.PrimaryForeground => "bit-smr-pfg",
            BitColor.SecondaryForeground => "bit-smr-sfg",
            BitColor.TertiaryForeground => "bit-smr-tfg",
            BitColor.PrimaryBorder => "bit-smr-pbr",
            BitColor.SecondaryBorder => "bit-smr-sbr",
            BitColor.TertiaryBorder => "bit-smr-tbr",
            _ => "bit-smr-tbg"
        });
    }



    private string GetWrapperClass()
    {
        var shapeClass = Circle ? "bit-smr-crl" : "bit-smr-lin";

        var colorClass = BackgroundColor switch
        {
            BitColor.Primary => "bit-smr-bpri",
            BitColor.Secondary => "bit-smr-bsec",
            BitColor.Tertiary => "bit-smr-bter",
            BitColor.Info => "bit-smr-binf",
            BitColor.Success => "bit-smr-bsuc",
            BitColor.Warning => "bit-smr-bwrn",
            BitColor.SevereWarning => "bit-smr-bswr",
            BitColor.Error => "bit-smr-berr",
            BitColor.PrimaryBackground => "bit-smr-bpbg",
            BitColor.SecondaryBackground => "bit-smr-bsbg",
            BitColor.TertiaryBackground => "bit-smr-btbg",
            BitColor.PrimaryForeground => "bit-smr-bpfg",
            BitColor.SecondaryForeground => "bit-smr-bsfg",
            BitColor.TertiaryForeground => "bit-smr-btfg",
            BitColor.PrimaryBorder => "bit-smr-bpbr",
            BitColor.SecondaryBorder => "bit-smr-bsbr",
            BitColor.TertiaryBorder => "bit-smr-btbr",
            _ => "bit-smr-bsbg"
        };

        return $"{shapeClass} {colorClass}";
    }

    private string GetAnimationClass() => Pulse ? "bit-smr-pul" : "bit-smr-wav";

    private string GetAnimationStyle()
    {
        var delay = AnimationDelay.HasValue ? $"animation-delay:{AnimationDelay}ms" : string.Empty;
        var duration = AnimationDuration.HasValue ? $"animation-duration:{AnimationDuration}ms" : string.Empty;

        return string.Join(';', [delay, duration]).Trim(';').Trim();
    }
}
