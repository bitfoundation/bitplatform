using System.Runtime.Versioning;
using Bit.BlazeDb.Samples.Core.Data;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// This project only ever runs in the browser, where the backends BrowserSampleDatabase opens exist.
[assembly: SupportedOSPlatform("browser")]

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The database lives here, in the browser tab: OPFS, or IndexedDB where the page cannot write OPFS.
builder.Services.AddCoreServices<BrowserSampleDatabase>();

await builder.Build().RunAsync();
