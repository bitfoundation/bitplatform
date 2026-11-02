using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// The render mode the pages run in: WebAssembly, as the site is published, or Server, which the browser suite asks for
/// (<c>Demo:RenderMode=Server</c>) to drive the same palette over a Blazor Server circuit.
/// </summary>
public sealed record DemoRenderMode(IComponentRenderMode Value, bool IsServer)
{
    public static DemoRenderMode From(string? setting) => string.Equals(setting, "Server", StringComparison.OrdinalIgnoreCase)
        ? new(RenderMode.InteractiveServer, IsServer: true)
        : new(RenderMode.InteractiveWebAssembly, IsServer: false);
}
