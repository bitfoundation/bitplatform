namespace Bit.Belemetry;

/// <summary>
/// The OpenTelemetry instrumentation scope a record was produced by - the <c>ActivitySource</c>, <c>Meter</c>
/// or logger category on the .NET side, or the instrumentation module on the browser side.
/// </summary>
/// <param name="Name">The scope name, e.g. <c>System.Net.Http</c> or <c>bit.belemetry.fetch</c>.</param>
/// <param name="Version">The scope version, when known.</param>
public sealed record BelemetryScope(string Name, string? Version = null)
{
    /// <summary>The scope used for records with no better-known origin.</summary>
    public static BelemetryScope Default { get; } = new("Bit.Belemetry");
}
