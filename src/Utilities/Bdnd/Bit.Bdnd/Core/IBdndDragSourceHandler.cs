namespace Bit.Bdnd;

/// <summary>What the source of the drag in progress does when the manager has to end that drag itself.</summary>
internal interface IBdndDragSourceHandler
{
    /// <summary>
    /// Ends drag <paramref name="dragId"/> at once, as a cancel: <c>OnEnd</c>'s synchronous part runs and the drag
    /// state is cleared before this returns; the task is the rest of <c>OnEnd</c>.
    /// </summary>
    Task AbortAsync(long dragId);
}
