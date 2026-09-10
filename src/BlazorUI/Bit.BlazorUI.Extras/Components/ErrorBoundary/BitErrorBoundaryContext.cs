namespace Bit.BlazorUI;

/// <summary>
/// What a <see cref="BitErrorBoundary.ErrorTemplate"/> is handed: the exception that was caught and
/// the three ways out of it that the boundary's own error UI offers.
/// </summary>
/// <remarks>
/// The inherited <c>ErrorContent</c> template receives nothing but the exception, so custom error UI
/// written against it has to reach back for a component reference before it can offer the reader a way
/// to recover. This context is what <see cref="BitErrorBoundary.ErrorTemplate"/> is given instead, so
/// that a retry button is one <c>@context.Recover</c> away.
/// </remarks>
public class BitErrorBoundaryContext
{
    internal BitErrorBoundaryContext(Exception exception, Action recover, Action refresh, Action goHome)
    {
        Exception = exception;
        Recover = recover;
        Refresh = refresh;
        GoHome = goHome;
    }



    /// <summary>
    /// The exception the boundary caught, which is the same instance the inherited <c>ErrorContent</c>
    /// template receives.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Clears the error and renders the boundary's content again, exactly like the default UI's
    /// Recover button.
    /// </summary>
    /// <remarks>
    /// The content is rendered from scratch, so whatever threw is run again: recovering into the same
    /// state throws the same exception straight back. Recover once the state behind the error has
    /// changed - or let <see cref="BitErrorBoundary.RecoverKeys"/> do it.
    /// </remarks>
    public Action Recover { get; }

    /// <summary>
    /// Reloads the current page in the browser, exactly like the default UI's Refresh button.
    /// </summary>
    public Action Refresh { get; }

    /// <summary>
    /// Navigates to <see cref="BitErrorBoundary.HomeUrl"/>, exactly like the default UI's Home button.
    /// </summary>
    public Action GoHome { get; }
}
