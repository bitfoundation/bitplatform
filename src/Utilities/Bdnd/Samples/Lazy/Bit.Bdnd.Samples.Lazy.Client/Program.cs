using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBdndServices here: Bit.Bdnd.wasm is not downloaded yet when this runs, and the call would need it. The
// draggables and drop targets create their interop themselves, and the board takes the same one from GetBdndInterop().

await builder.Build().RunAsync();
