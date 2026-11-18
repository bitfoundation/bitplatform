namespace Bit.BevTools;

public sealed class BevToolsJsCallEntry
{
    public string? Identifier { get; set; }

    public int Args { get; set; }

    public bool Sync { get; set; }

    public double DurationMs { get; set; }

    public string? Error { get; set; }

    public long At { get; set; }

    /// <summary>How long before the report the call was made; null from a hub that does not say.</summary>
    public double? AgeMs { get; set; }

    /// <summary>Made while a requestAnimationFrame callback ran; false from a hub that does not say.</summary>
    public bool Frame { get; set; }

    /// <summary>
    /// How many calls the entry stands for: the hub folds the calls of a report window past the ones it reports one by
    /// one into an entry per kind of call (identifier, second, frame, sync, argument count, failed or not), so a page
    /// calling .NET thousands of times never sends a report the circuit refuses. <see cref="DurationMs"/> is then the
    /// longest of them, <see cref="TotalMs"/> their sum, <see cref="At"/> the time of the first. Null (one call) from a
    /// hub that does not fold.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>The sum of the durations of the calls a folded entry stands for; null for one call.</summary>
    public double? TotalMs { get; set; }

    internal int Calls => Math.Max(1, Count ?? 1);

    internal double TotalDurationMs => Calls == 1 ? DurationMs : TotalMs ?? DurationMs;
}
