namespace Bit.BlazorUI;

/// <summary>
/// What took a <see cref="BitErrorBoundary"/> out of its errored state, handed to
/// <see cref="BitErrorBoundary.OnRecover"/>.
/// </summary>
/// <remarks>
/// A boundary recovers by three different routes, and what the app should do about it is rarely the
/// same for all three: a reader who asked to try again is waiting for something to happen, while a
/// boundary that cleared itself because a key changed or because the reader left the page has nothing
/// to report. The reason is what tells them apart without a flag of the page's own.
/// </remarks>
public enum BitErrorBoundaryRecoverReason
{
    /// <summary>
    /// The Recover button of the default error UI, the <c>Recover</c> action of an
    /// <see cref="BitErrorBoundary.ErrorTemplate"/>'s context, or a call to
    /// <see cref="BitErrorBoundary.Recover"/>.
    /// </summary>
    Manual,

    /// <summary>
    /// One of the values of <see cref="BitErrorBoundary.RecoverKeys"/> differed from what the boundary
    /// last saw.
    /// </summary>
    Keys,

    /// <summary>
    /// The reader navigated to another location while <see cref="BitErrorBoundary.RecoverOnNavigation"/>
    /// was set.
    /// </summary>
    Navigation,
}
