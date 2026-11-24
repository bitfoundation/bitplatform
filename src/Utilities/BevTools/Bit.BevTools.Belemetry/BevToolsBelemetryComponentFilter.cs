using Bit.Belemetry;

namespace Bit.BevTools;

/// <summary>
/// BevTools' own components for Belemetry's diagnostic timeline: a UI event BevTools' UI handles (the Support panel's
/// Capture button, a tab) is BevTools' activity, never the application's - the rule BevTools applies to itself: the same
/// exact namespace, and the types the session has seen rendered only by BevTools' UI, which covers a panel an extension
/// or the application registered from its own namespace and what that panel renders.
/// </summary>
internal sealed class BevToolsBelemetryComponentFilter : IBelemetryDiagnosticComponentFilter
{
    public bool IsExcluded(string componentType) =>
        BevToolsActivityObserver.IsBevToolsOwnComponent(componentType)
        || (BevToolsSessionResolver.Resolve() is { } session
            && session.Components.IsBevToolsOnlyType(BevToolsActivityObserver.TypeKey(componentType)));
}
