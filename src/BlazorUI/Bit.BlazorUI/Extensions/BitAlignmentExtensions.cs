namespace Bit.BlazorUI;

/// <summary>
/// Which members of <see cref="BitAlignment"/> mean something on which axis of a flex or grid container, kept in one place
/// so every layout component that splits the enum over justify-content and align-items drops the same members.
/// </summary>
internal static class BitAlignmentExtensions
{
    /// <summary>
    /// The alignment as it applies to the axis the children are laid out along, or null when it means nothing there.
    /// </summary>
    /// <remarks>
    /// Lining children up on their first line of text says nothing about that axis, and neither does stretching them - a flex
    /// container lays justify-content:stretch out as flex-start - so Baseline and Stretch are dropped, which is also what lets
    /// the axis fall through to a shorthand instead of being silenced by a value that was never about it.
    /// </remarks>
    internal static BitAlignment? ForMainAxis(this BitAlignment? alignment)
    {
        return alignment is BitAlignment.Baseline or BitAlignment.Stretch ? null : alignment;
    }

    /// <summary>
    /// The alignment as it applies to the axis across the children, or null when it means nothing there.
    /// </summary>
    /// <remarks>
    /// Sharing room out between the children says nothing about the axis across them, so the three space distributions are
    /// dropped rather than written out as an align-items the browser throws away.
    /// </remarks>
    internal static BitAlignment? ForCrossAxis(this BitAlignment? alignment)
    {
        return alignment is BitAlignment.SpaceBetween or BitAlignment.SpaceAround or BitAlignment.SpaceEvenly ? null : alignment;
    }
}
