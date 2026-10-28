using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI.Demo.Client.Core.Services;

/// <summary>
/// The one render mode the whole site runs in. App.razor hands it to both Routes and HeadOutlet, so
/// a page never declares one of its own.
/// </summary>
public static class AppRenderMode
{
    // Every page is prerendered: the components render their initial, accessible markup on the server,
    // so a visitor (and the E2E suite) reads the page before the runtime is interactive. ?no-prerender
    // switches it off per request (see App.razor).
    public static readonly bool PrerenderEnabled = true;

    public static IComponentRenderMode BlazorWebAssembly { get; } = new InteractiveWebAssemblyRenderMode(PrerenderEnabled);
    public static IComponentRenderMode BlazorServer { get; } = new InteractiveServerRenderMode(PrerenderEnabled);
    public static IComponentRenderMode NoPrerenderBlazorWebAssembly { get; } = new InteractiveWebAssemblyRenderMode(prerender: false);
    public static IComponentRenderMode NoPrerenderBlazorServer { get; } = new InteractiveServerRenderMode(prerender: false);

    // Whether the app boots as Blazor WebAssembly rather than Blazor Server. Release always does; in Debug
    // the default is Blazor Server for a faster inner loop, unless the build opts the WASM client in with
    // -p:IncludeWasm=true (which defines INCLUDE_WASM, see the csproj). The WASM client itself is always
    // WebAssembly (OperatingSystem.IsBrowser()).
    public static bool WasmEnabled { get; set; } =
#if INCLUDE_WASM || !DEBUG
        true;
#else
        OperatingSystem.IsBrowser();
#endif

    public static IComponentRenderMode Current => WasmEnabled ? BlazorWebAssembly : BlazorServer;
}
