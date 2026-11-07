namespace Bit.Belemetry;

/// <summary>
/// Leaves a tool's own components out of the diagnostic timeline. A developer tool rendered in the app (BevTools' panels)
/// handles UI events through the same renderer as the app, and the framework reports them alike: without a filter, a
/// click on the tool's Capture button would be on the timeline - and in the capture - as the app's activity. The
/// framework's <c>HandleEvent</c> and <c>Navigate</c> activities naming a component type a filter excludes are not
/// recorded. Register it as a singleton service; Belemetry never knows the tool otherwise.
/// </summary>
public interface IBelemetryDiagnosticComponentFilter
{
    /// <summary>
    /// Whether <paramref name="componentType"/> (a full type name, as the framework's activities name it) is the tool's
    /// own. Called for every framework activity while diagnostics are on: it must be fast, and never throw (a filter that
    /// throws excludes nothing).
    /// </summary>
    bool IsExcluded(string componentType);
}
