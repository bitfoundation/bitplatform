using Bit.Belemetry;

namespace Bit.BevTools;

/// <summary>
/// BevTools' own components for Belemetry's diagnostic timeline: a UI event BevTools' UI handles (the Support panel's
/// Capture button, a tab) is BevTools' activity, never the application's - the rule BevTools applies to itself, by the
/// same exact namespace.
/// </summary>
internal sealed class BevBelemetryComponentFilter : IBelemetryDiagnosticComponentFilter
{
    public bool IsExcluded(string componentType) => ActivityObserver.IsBevToolsOwnComponent(componentType);
}
