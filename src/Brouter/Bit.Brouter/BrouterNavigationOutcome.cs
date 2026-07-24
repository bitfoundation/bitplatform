namespace Bit.Brouter;

/// <summary>How a navigation started with <see cref="IBrouter.NavigateAsync"/> concluded.</summary>
public enum BrouterNavigationStatus
{
    /// <summary>The navigation committed and the matched route rendered.</summary>
    Succeeded = 0,

    /// <summary>A guard or <c>OnNavigating</c> hook cancelled the navigation; the URL is unchanged (or restored).</summary>
    Cancelled = 1,

    /// <summary>A guard, hook or <see cref="Broute.RedirectTo"/> sent the navigation elsewhere; see <see cref="BrouterNavigationOutcome.RedirectedTo"/>.</summary>
    Redirected = 2,

    /// <summary>No route matched the target URL; the not-found handling (fallback content or NotFound redirect) took over.</summary>
    NotFound = 3,

    /// <summary>A guard or loader threw; see <see cref="BrouterNavigationOutcome.Exception"/>. Error boundaries / <c>OnError</c> observed it.</summary>
    Failed = 4,

    /// <summary>A newer navigation started before this one finished, or the router was disposed.</summary>
    Superseded = 5,
}

/// <summary>
/// The result of an awaited navigation (<see cref="IBrouter.NavigateAsync"/>), mirroring Vue
/// Router's navigation failures: callers can branch on how the navigation actually ended instead
/// of assuming it committed.
/// </summary>
public readonly struct BrouterNavigationOutcome
{
    private BrouterNavigationOutcome(BrouterNavigationStatus status, string? redirectedTo, Exception? exception)
    {
        Status = status;
        RedirectedTo = redirectedTo;
        Exception = exception;
    }

    /// <summary>How the navigation concluded.</summary>
    public BrouterNavigationStatus Status { get; }

    /// <summary>The URL a redirecting guard/hook/route sent the navigation to, when <see cref="Status"/> is <see cref="BrouterNavigationStatus.Redirected"/>.</summary>
    public string? RedirectedTo { get; }

    /// <summary>The failure, when <see cref="Status"/> is <see cref="BrouterNavigationStatus.Failed"/>.</summary>
    public Exception? Exception { get; }

    /// <summary>Convenience: true when <see cref="Status"/> is <see cref="BrouterNavigationStatus.Succeeded"/>.</summary>
    public bool Succeeded => Status == BrouterNavigationStatus.Succeeded;

    internal static BrouterNavigationOutcome Success() => new(BrouterNavigationStatus.Succeeded, null, null);
    internal static BrouterNavigationOutcome Cancelled() => new(BrouterNavigationStatus.Cancelled, null, null);
    internal static BrouterNavigationOutcome Redirected(string? to) => new(BrouterNavigationStatus.Redirected, to, null);
    internal static BrouterNavigationOutcome NotFound() => new(BrouterNavigationStatus.NotFound, null, null);
    internal static BrouterNavigationOutcome Failed(Exception ex) => new(BrouterNavigationStatus.Failed, null, ex);
    internal static BrouterNavigationOutcome Superseded() => new(BrouterNavigationStatus.Superseded, null, null);
}
