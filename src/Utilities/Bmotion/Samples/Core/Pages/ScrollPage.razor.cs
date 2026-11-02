namespace Bit.Bmotion.Samples.Core.Pages;

public partial class ScrollPage
{
    private static readonly BmPreset RevealFromLeft = BmPresets.Reveal(from: BmEdge.Left);
    private static readonly BmPreset RevealFromRight = BmPresets.Reveal(from: BmEdge.Right);

    private static readonly string[] Reveals =
    [
        "Each line waits off its place until the viewport reaches it.",
        "Each line plays once, the first time it enters.",
        "Scrolling back up leaves them where they are.",
        "With reduced motion they fade in without moving.",
    ];
}
