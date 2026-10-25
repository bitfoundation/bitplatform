using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// The demo's own <see cref="ActivitySource"/> and <see cref="Meter"/>, exactly as an app declares
/// them: plain BCL types with no reference to Belemetry. They are collected because their name is in
/// <c>options.Traces.Sources</c> and <c>options.Metrics.Meters</c> (see <see cref="DemoTelemetry"/>).
/// </summary>
public static class DemoDiagnostics
{
    /// <summary>The name of both the source and the meter.</summary>
    public const string Name = "Bit.Belemetry.Demo";

    public static readonly ActivitySource Source = new(Name, "1.0.0");

    public static readonly Meter Meter = new(Name, "1.0.0");

    /// <summary>How many times a reader pressed a demo button, by button.</summary>
    public static readonly Counter<long> ButtonPresses = Meter.CreateCounter<long>("demo.button.presses", "{press}", "Demo buttons pressed, by button.");

    /// <summary>How long the simulated work of the .NET page took.</summary>
    public static readonly Histogram<double> WorkDuration = Meter.CreateHistogram<double>("demo.work.duration", "ms", "Duration of the simulated work.");

    /// <summary>Items in the simulated cart, up and down.</summary>
    public static readonly UpDownCounter<long> CartItems = Meter.CreateUpDownCounter<long>("demo.cart.items", "{item}", "Items in the demo cart.");
}
