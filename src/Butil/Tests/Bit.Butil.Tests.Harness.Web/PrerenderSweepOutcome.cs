namespace ButilTests.Harness.Web;

/// <summary>What happened to one member <see cref="PrerenderSweep"/> called.</summary>
public enum PrerenderSweepOutcome
{
    /// <summary>Returned (and, where it returned something disposable, was disposed) without throwing.</summary>
    Completed,

    /// <summary>
    /// Threw an <see cref="ArgumentException"/> - the sweep's made-up arguments were refused, which is argument
    /// validation working - or an exception type Bit.Butil declares itself, which is a failure mode the member
    /// documents. Neither is a prerender failure.
    /// </summary>
    Rejected,

    /// <summary>Not called: a parameter has a type the sweep cannot make a value of.</summary>
    Skipped,

    /// <summary>Threw anything else - exactly what Bit.Butil promises never happens during prerendering.</summary>
    Failed,

    /// <summary>Did not finish in time: a prerender call that waits on JavaScript that will never answer.</summary>
    Hung,
}
