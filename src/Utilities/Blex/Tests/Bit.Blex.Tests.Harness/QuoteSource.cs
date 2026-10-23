namespace Bit.Blex.Tests.Harness;

/// <summary>
/// The "server" the query page caches: every answer is new, and each runtime counts the requests it sent,
/// so a test can tell an answer handed over from the prerender apart from one fetched again.
/// </summary>
public sealed class QuoteSource
{
    /// <summary>The requests this scope's runtime sent.</summary>
    public int Requests { get; private set; }

    public async Task<string> FetchAsync(CancellationToken cancellationToken)
    {
        Requests++;
        await Task.Delay(50, cancellationToken);
        return $"{HarnessRuntime.Platform}-{Guid.NewGuid():N}";
    }

    /// <summary>The requests the always-stale "tick" query sent, counted apart from the quote's.</summary>
    public int Ticks { get; private set; }

    public async Task<string> FetchTickAsync(CancellationToken cancellationToken)
    {
        var tick = ++Ticks;
        await Task.Delay(10, cancellationToken);
        return $"{HarnessRuntime.Platform}-tick-{tick}";
    }
}
