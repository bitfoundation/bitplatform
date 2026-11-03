using System.Reflection;

namespace Boilerplate.WorkbenchTests.Infrastructure;

/// <summary>
/// The four ways to use the Workbench: on a phone or a desktop screen, in dev mode with an agent, or as a copy without one, like a
/// published demo. They differ in layout and in what may change, so a feature is tested on each of them.
/// </summary>
public enum WorkbenchSurface
{
    DesktopWithAgent,
    DesktopWithoutAgent,
    PhoneWithAgent,
    PhoneWithoutAgent
}

/// <summary>
/// Runs a browser test once per surface, each on its own server and screen. The test takes the surface as its parameter and
/// <see cref="WorkbenchPageTest"/> sets everything up for it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SurfacesAttribute(params WorkbenchSurface[] surfaces) : Attribute, ITestDataSource
{
    public IEnumerable<object?[]> GetData(MethodInfo methodInfo) => surfaces.Select(surface => new object?[] { surface });

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data) => $"{methodInfo.Name} ({data?[0]})";
}

/// <summary>A feature every surface has.</summary>
public sealed class EverySurfaceAttribute() : SurfacesAttribute(WorkbenchSurface.DesktopWithAgent, WorkbenchSurface.DesktopWithoutAgent, WorkbenchSurface.PhoneWithAgent, WorkbenchSurface.PhoneWithoutAgent);

/// <summary>A feature only a desktop screen offers, with an agent or without one.</summary>
public sealed class OnDesktopsAttribute() : SurfacesAttribute(WorkbenchSurface.DesktopWithAgent, WorkbenchSurface.DesktopWithoutAgent);

/// <summary>A feature only a phone screen offers, with an agent or without one.</summary>
public sealed class OnPhonesAttribute() : SurfacesAttribute(WorkbenchSurface.PhoneWithAgent, WorkbenchSurface.PhoneWithoutAgent);

/// <summary>A feature only dev mode has, on either screen.</summary>
public sealed class WithAnAgentAttribute() : SurfacesAttribute(WorkbenchSurface.DesktopWithAgent, WorkbenchSurface.PhoneWithAgent);

/// <summary>What a copy without an agent does instead, on either screen.</summary>
public sealed class WithoutAnAgentAttribute() : SurfacesAttribute(WorkbenchSurface.DesktopWithoutAgent, WorkbenchSurface.PhoneWithoutAgent);
