namespace Bit.Blow;

/// <summary>Problem codes a run is refused with, in <see cref="BlowRunResult.Problems"/>.</summary>
public static class BlowRunCodes
{
    /// <summary>A node has no handler and <see cref="BlowRunnerOptions.MissingHandler"/> refuses.</summary>
    public const string MissingHandler = "missing-handler";

    /// <summary>A start node (<see cref="BlowRunRequest.StartAt"/>) is not in the graph.</summary>
    public const string UnknownStart = "unknown-start";

    /// <summary>Runs nested deeper than <see cref="BlowRunnerOptions.MaxDepth"/>.</summary>
    public const string TooDeep = "too-deep";
}
