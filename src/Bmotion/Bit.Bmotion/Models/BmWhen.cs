namespace Bit.Bmotion;

/// <summary>
/// Orders a variant container's own animation against its children's - motion.dev's
/// <c>when</c>. Set it on the transition of an element that declares <c>Variants</c>; it has no
/// effect anywhere else.
/// </summary>
public enum BmWhen
{
    /// <summary>
    /// The container and its children animate at the same time (the default), with the children
    /// offset only by <c>DelayChildren</c> / <c>StaggerChildren</c>.
    /// </summary>
    Together,

    /// <summary>
    /// The container finishes first: every child's delay is pushed back by the container's own
    /// animation duration, on top of <c>DelayChildren</c>. This is the "the panel opens, then its
    /// contents cascade in" order, and it stays correct when the container's duration changes -
    /// unlike hand-computing the same offset into <c>DelayChildren</c>.
    /// <para>
    /// A spring has no true end, so its duration is taken from <c>Bm.Spring(duration:)</c> when set
    /// and estimated from the physics otherwise.
    /// </para>
    /// </summary>
    BeforeChildren,

    /// <summary>
    /// The container goes last: its own animation is delayed until the whole child cascade has
    /// finished - the latest child's stagger delay plus that child's own animation duration. This is
    /// the "let the contents leave, then close the panel" order.
    /// <para>
    /// The wait is computed from the children that actually registered with this container and
    /// each child's own resolved transition, so it tracks the real cascade rather than a guess. A
    /// child whose animation length can't be known ahead of time (an infinite repeat) contributes
    /// only its delay.
    /// </para>
    /// </summary>
    AfterChildren,
}
