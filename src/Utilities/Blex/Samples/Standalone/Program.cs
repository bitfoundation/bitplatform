using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bit.Blex.Samples.Core;

// One project, one DI container, no prerendering: the browser downloads the app and renders it from
// scratch, so the root component and the HeadOutlet are registered here rather than in a host page.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddWebServices();

await builder.Build().RunAsync();
