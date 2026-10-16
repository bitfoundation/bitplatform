using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bit.Belemetry.Tests.Harness;

/// <summary>The harness's own <see cref="ActivitySource"/> and <see cref="Meter"/>, both registered with Belemetry.</summary>
public static class HarnessTelemetry
{
    public const string Name = "Bit.Belemetry.Tests.Harness";

    /// <summary>The attribute every record a harness button produces carries, so a test finds its own records.</summary>
    public const string MarkerAttribute = "harness.marker";

    public static ActivitySource ActivitySource { get; } = new(Name);

    public static Meter Meter { get; } = new(Name);

    public static Counter<long> Clicks { get; } = Meter.CreateCounter<long>("harness.clicks");
}
