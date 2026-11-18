namespace Bit.Belemetry;

/// <summary>What sanitizing a value did: how many values were redacted, cut and left out.</summary>
internal struct BelemetrySanitizeCounters
{
    public int Redactions;
    public int Truncations;
    public int Omissions;

    public readonly bool Any => Redactions + Truncations + Omissions > 0;
}
