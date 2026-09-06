namespace Bit.Butil;

/// <summary>
/// How urgent a task posted through <see cref="Scheduler.PostTask"/> is, matching the strings
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Scheduler/postTask">scheduler.postTask</see>
/// accepts.
/// </summary>
/// <remarks>
/// This is the thing a timeout cannot express. Every <c>setTimeout</c> lands in one queue in the
/// order it was scheduled; these land in three, and the browser drains them against its own
/// rendering work rather than around it.
/// </remarks>
public enum SchedulerPriority
{
    /// <summary>
    /// Run ahead of rendering - the user is waiting for the result. Reserve it for work whose delay
    /// the user would see as the page being slow, because everything in this queue delays painting.
    /// </summary>
    UserBlocking,

    /// <summary>
    /// The default. Work the user will notice, but not before the next frame - filling in a list,
    /// preparing the next view.
    /// </summary>
    UserVisible,

    /// <summary>
    /// Run when nothing better is waiting: logging, prefetching, cleanup. A page under load may put
    /// this off for a long time, which is the point.
    /// </summary>
    Background,
}
