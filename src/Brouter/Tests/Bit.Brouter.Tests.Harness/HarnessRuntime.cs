namespace Bit.Brouter.Tests.Harness;

public static class HarnessRuntime
{
    /// <summary>
    /// "browser" inside the WebAssembly runtime, "dotnet" everywhere else (prerender, Server
    /// circuits, BlazorWebView). Unlike <c>RendererInfo</c> this exists on every target framework.
    /// </summary>
    public static string Platform => OperatingSystem.IsBrowser() ? "browser" : "dotnet";
}
