using Bit.Butil;
using Bit.Butil.Samples.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddWebServices();

builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var host = builder.Build();

// The E2E suite drives this app in both of Bit.Butil's script-loading modes. `?lazy=1` on the start URL
// switches to lazy scripts (index.html then leaves the bit-butil.js <script> tag out, so the only way any
// BitButil.* namespace can appear on the page is through the per-module import()s the library performs).
// A real app would set <BitButilLazyScripts>true</BitButilLazyScripts> in its csproj instead of doing this
// at runtime; the query-string switch is only so one build of the sample can be tested both ways. The test
// is for a parameter with that exact name and value, and not for "lazy=1" appearing anywhere in the query,
// so that it agrees with index.html: any other value (?lazy=0 included) is bundle mode on both sides.
var startUri = new Uri(host.Services.GetRequiredService<NavigationManager>().Uri);
if (startUri.Query.TrimStart('?').Split('&').Contains("lazy=1", StringComparer.Ordinal))
{
    BitButil.UseLazyScripts();
}

await host.RunAsync();
