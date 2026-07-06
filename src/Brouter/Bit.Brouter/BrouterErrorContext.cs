using System.Threading.Tasks;

namespace Bit.Brouter;

/// <summary>
/// The context handed to an <c>ErrorContent</c> fragment (<see cref="Broute.ErrorContent"/> or
/// <see cref="Brouter.ErrorContent"/>) when a navigation fails in the commit phase - typically a
/// route <see cref="Broute.Loader"/> throwing. Carries the failure and the location it happened
/// for, plus <see cref="RetryAsync"/> to re-run the failed navigation in place.
/// Inspired by React Router's <c>ErrorBoundary</c>/<c>useRouteError</c> and SvelteKit's
/// <c>+error.svelte</c> hierarchy.
/// </summary>
public sealed class BrouterErrorContext
{
    private readonly Brouter _brouter;

    internal BrouterErrorContext(Exception exception, BrouterLocation location, Brouter brouter)
    {
        Exception = exception;
        Location = location;
        _brouter = brouter;
    }

    /// <summary>The exception that failed the navigation.</summary>
    public Exception Exception { get; }

    /// <summary>The location whose navigation failed (the URL the user is at).</summary>
    public BrouterLocation Location { get; }

    /// <summary>
    /// Re-runs the full navigation pipeline for the current URL - guards, loaders and render - so a
    /// transient failure (e.g. a flaky fetch inside a <see cref="Broute.Loader"/>) can be retried
    /// without leaving the page. A successful retry replaces the error UI with the routed content;
    /// another failure re-renders the error UI with the new exception.
    /// </summary>
    public Task RetryAsync() => _brouter.RetryNavigationAsync();
}
