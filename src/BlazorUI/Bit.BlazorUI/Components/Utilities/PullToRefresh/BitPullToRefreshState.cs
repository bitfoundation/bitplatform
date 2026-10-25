namespace Bit.BlazorUI;

/// <summary>
/// The stage of the gesture a <see cref="BitPullToRefresh"/> is at.
/// </summary>
/// <remarks>
/// A pull moves from <see cref="Idle"/> to <see cref="Pulling"/>, and on to <see cref="CanRelease"/> once it has
/// passed the trigger. Letting go there runs the refresh (<see cref="Refreshing"/>), which a positive CompleteDelay
/// follows with <see cref="Complete"/>; every other way the gesture ends goes straight back to <see cref="Idle"/>.
/// </remarks>
public enum BitPullToRefreshState
{
    /// <summary>
    /// Nothing is being pulled and no refresh is running.
    /// </summary>
    Idle,

    /// <summary>
    /// A pull is under way but has not reached the trigger, so letting go drops it.
    /// </summary>
    Pulling,

    /// <summary>
    /// The pull has passed the trigger, so letting go starts the refresh.
    /// </summary>
    CanRelease,

    /// <summary>
    /// The refresh is running: the OnRefresh callback has not returned yet.
    /// </summary>
    Refreshing,

    /// <summary>
    /// The refresh has finished and the complete indicator is held open for the CompleteDelay.
    /// </summary>
    Complete
}
