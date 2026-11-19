using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BevTools;

/// <summary>Names render modes the way they are written in markup.</summary>
public static class BevToolsRenderModes
{
    /// <summary>"InteractiveServer", "InteractiveWebAssembly", "InteractiveAuto", with ", no prerendering" when it is off; null for no mode.</summary>
    public static string? Describe(IComponentRenderMode? mode) => mode switch
    {
        null => null,
        InteractiveServerRenderMode server => "InteractiveServer" + (server.Prerender ? "" : ", no prerendering"),
        InteractiveWebAssemblyRenderMode wasm => "InteractiveWebAssembly" + (wasm.Prerender ? "" : ", no prerendering"),
        InteractiveAutoRenderMode auto => "InteractiveAuto" + (auto.Prerender ? "" : ", no prerendering"),
        _ => BevToolsTypeNames.Short(mode.GetType()).Replace("RenderMode", ""),
    };
}
