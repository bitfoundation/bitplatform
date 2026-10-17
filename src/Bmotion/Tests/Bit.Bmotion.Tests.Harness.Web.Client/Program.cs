using Bit.Bmotion;
using BmotionTests.Harness.Web.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The library registers the options instance it is handed as a singleton, so keeping hold of it lets the
// options be filled in once the host - and with it the JS runtime that can read the page - exists.
BitBmotionOptions bmotionOptions = default!;
builder.Services.AddBitBmotionServices(o => bmotionOptions = o);

var host = builder.Build();

// The server half decides the library options (BmotionHarness:*) and writes them on <body data-bm-*>: a
// WebAssembly client has no configuration of its own to read them from, and the two halves must agree.
var js = (IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>();
HarnessOptions.Read(key => js.Invoke<string?>("document.body.getAttribute", key switch
{
    HarnessOptions.PrerenderKey => HarnessOptions.PrerenderAttribute,
    HarnessOptions.ReducedMotionKey => HarnessOptions.ReducedMotionAttribute,
    _ => HarnessOptions.CssSafeModeAttribute,
})).Apply(bmotionOptions);

await host.RunAsync();
