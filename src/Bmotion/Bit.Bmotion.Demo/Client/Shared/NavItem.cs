namespace Bit.Bmotion.Demo.Client.Shared;

/// <summary>
/// One entry of the docs navigation. The list itself is <see cref="All"/>: the nav panel renders
/// it and the search box filters it, so both the drawer and the sticky rail always show the same
/// set of pages in the same order.
/// </summary>
/// <param name="Title">The label shown in the nav panel.</param>
/// <param name="Href">The route, relative to the app base.</param>
public sealed record NavItem(string Title, string Href)
{
    /// <summary>Every demo page, in the order they are meant to be read.</summary>
    public static readonly NavItem[] All =
    [
        new("Home", ""),
        new("Basics", "basic"),
        new("Springs", "springs"),
        new("Easing", "easing"),
        new("Gestures", "gestures"),
        new("Variants", "variants"),
        new("Keyframes", "keyframes"),
        new("Split Text", "split-text"),
        new("AnimatePresence", "presence"),
        new("Drag", "drag"),
        new("Reorder", "reorder"),
        new("Scroll", "scroll"),
        new("Layout", "layout"),
        new("Programmatic", "programmatic"),
        new("Color", "color"),
        new("Motion Path", "motion-path"),
        new("View Transitions", "view-transitions"),
        new("CSS Props", "css-props"),
        new("Accessibility", "accessibility"),
    ];
}
