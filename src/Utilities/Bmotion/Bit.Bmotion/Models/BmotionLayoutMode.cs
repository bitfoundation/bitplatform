namespace Bit.Bmotion;

/// <summary>What a layout (FLIP) animation animates.</summary>
public enum BmotionLayoutMode
{
    /// <summary>
    /// Animate both position and size (translate + scale). Direct children are counter-scaled and the
    /// border-radius is corrected each frame, so text/children don't stretch and corners stay round.
    /// </summary>
    Full,
    /// <summary>Animate position only - the cheapest, always-distortion-free option.</summary>
    Position,
    /// <summary>
    /// Animate size only: the element snaps to its new position and scales from its old size, with
    /// the same child counter-scaling and border-radius correction as <see cref="Full"/>. Use it
    /// when the element's box grows or shrinks in place and the move itself shouldn't be animated
    /// (an accordion panel, a resizing card).
    /// </summary>
    Size,
}
