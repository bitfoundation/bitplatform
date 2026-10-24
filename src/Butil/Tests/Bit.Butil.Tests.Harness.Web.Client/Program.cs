using Bit.Butil;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCoreServices();

var host = builder.Build();

// The server half decides the script-loading mode (ButilHarness:Scripts) and writes it on <body data-scripts>:
// a WebAssembly client has no configuration of its own to read it from, and the two halves must agree - the
// page has no bundle on it in lazy mode, so a client left in bundle mode would find no BitButil.* at all.
var js = (IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>();
if (js.Invoke<string?>("document.body.getAttribute", "data-scripts") == "lazy")
{
    BitButil.UseLazyScripts();
}

await host.RunAsync();
