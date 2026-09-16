namespace ButilTests.Harness.Web;

/// <summary>One member <see cref="PrerenderSweep"/> called, and how the call went.</summary>
public sealed record PrerenderSweepEntry(string Service, string Member, PrerenderSweepOutcome Outcome, string? Detail);
