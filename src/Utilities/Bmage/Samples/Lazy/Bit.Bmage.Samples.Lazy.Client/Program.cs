using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBmageServices here: Bit.Bmage.wasm and Bit.Bmage.Blazor.wasm are not downloaded yet when this runs, and the
// call would need them. BmageEditor, the app's only use of Bmage, builds the services it needs itself, with Bmage's
// defaults.

await builder.Build().RunAsync();
