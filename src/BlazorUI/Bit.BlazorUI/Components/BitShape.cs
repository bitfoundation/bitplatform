namespace Bit.BlazorUI;

/// <summary>
/// The outline a component draws itself with: how much its corners are rounded, and - for
/// <see cref="Circle"/> alone - what proportions it takes.
/// </summary>
/// <remarks>
/// Every value but <see cref="Circle"/> only sets a corner radius and leaves the size of the box to the
/// component, so the shape a reader sees also depends on how wide the box is: a <see cref="Pill"/> is a pill
/// where the box is wider than it is tall and a circle where the box is square, which is why the round coin
/// of a BitPersona and the round counter of a BitBadge are both pills.
/// <br />
/// Not every component honours every value. Each parameter names the ones it accepts, and falls back to its
/// own default for the rest.
/// </remarks>
public enum BitShape
{
    /// <summary>
    /// The corner radius the current theme gives to this kind of surface, which is what most components draw
    /// unless something says otherwise.
    /// </summary>
    Rounded,

    /// <summary>
    /// Sharp corners with no radius at all, for content that meets its container edge to edge.
    /// </summary>
    Square,

    /// <summary>
    /// Fully rounded ends, whatever the theme says: a pill where the box is wider than it is tall, and a
    /// circle where the box is square.
    /// </summary>
    Pill,

    /// <summary>
    /// A true circle, which takes its diameter from whichever of the height and the width is set rather than
    /// leaving the proportions of the box alone. It is the only value that changes the size of what it draws.
    /// </summary>
    Circle
}
