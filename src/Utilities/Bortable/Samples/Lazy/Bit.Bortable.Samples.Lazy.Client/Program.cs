using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBortableServices here: Bit.Bortable.wasm is not downloaded yet when this runs, and the call would need it. The
// project file sets the Bit.Bortable.Unregistered switch instead, so the lists create their drag coordinator themselves.

await builder.Build().RunAsync();
