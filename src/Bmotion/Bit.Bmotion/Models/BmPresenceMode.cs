namespace Bit.Bmotion;

/// <summary>How presence components sequence exit and enter animations.</summary>
public enum BmPresenceMode
{
    /// <summary>Exiting and entering content animate at the same time.</summary>
    Sync,
    /// <summary>The exit animation finishes before the new content enters.</summary>
    Wait,
    /// <summary>
    /// Like <see cref="Sync"/>, but exiting elements "pop" out of the layout flow
    /// (<c>position: absolute</c>, pinned at their current spot) so surrounding content reflows
    /// immediately while the exit animation plays - motion.dev's <c>mode="popLayout"</c>.
    /// The nearest positioned ancestor is the reference: give the container
    /// <c>position: relative</c> for predictable pinning.
    /// </summary>
    PopLayout,
}
