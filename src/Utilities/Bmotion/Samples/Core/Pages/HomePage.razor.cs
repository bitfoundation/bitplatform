namespace Bit.Bmotion.Samples.Core.Pages;

public partial class HomePage
{
    private bool _shown = true;

    // The transition on the container staggers the tiles under it. Going, the container fades only once the last tile
    // has left: fading with them, it would hide the cascade while most of it is still to come.
    private static readonly BmTransition ShowTiles = Bm.Tween(0.3, staggerChildren: 0.07);
    private static readonly BmTransition HideTiles = Bm.Tween(0.3, staggerChildren: 0.07, when: BmWhen.AfterChildren);

    // The container's own fade.
    private readonly BmVariants _list = new()
    {
        ["hidden"] = Bm.To(opacity: 0),
        ["visible"] = Bm.To(opacity: 1),
    };

    private readonly BmVariants _tile = new()
    {
        ["hidden"] = Bm.To(opacity: 0, y: 16),
        ["visible"] = Bm.To(opacity: 1, y: 0, transition: Bm.Spring(bounce: 0.3, duration: 0.5)),
    };

    private readonly Sample[] _samples =
    [
        new("gestures", "Gestures", "Hover, press and keyboard focus, a drag kept inside its box, and a slider that settles where a throw would land."),
        new("presence", "Presence", "A dialog and toasts that animate out before Blazor removes them, and steps that swap one for another."),
        new("layout", "Layout", "FLIP animations when a re-render moves things, a tab underline shared across elements, and drag-to-reorder."),
        new("programmatic", "Programmatic", "Animating by selector with controls to pause and scrub, a sequence, and what needs the per-frame loop."),
        new("scroll", "Scroll", "The page's scroll driving animations on the browser's own timeline, and the scroll position as a number in C#."),
    ];

    private sealed record Sample(string Href, string Title, string Summary);
}
