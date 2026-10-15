using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bit.Belemetry.Samples.Core;

/// <summary>
/// The app's own instrumentation. Belemetry collects any <see cref="ActivitySource"/> and <see cref="Meter"/> once its
/// name is listed in <c>BelemetryOptions.Traces.Sources</c> / <c>Metrics.Meters</c> - see <see cref="Name"/>.
/// </summary>
public static class SampleTelemetry
{
    /// <summary>The name of both the activity source and the meter.</summary>
    public const string Name = "Bit.Belemetry.Samples";

    public static ActivitySource ActivitySource { get; } = new(Name);

    public static Meter Meter { get; } = new(Name);

    public static Counter<long> ButtonClicks { get; } = Meter.CreateCounter<long>("samples.button.clicks", unit: "{click}", description: "Clicks on the sample buttons.");
}
