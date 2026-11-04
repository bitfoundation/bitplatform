using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBcrdtServices here: Bit.Bcrdt.wasm is not downloaded yet when this runs, and the call would need it. The
// notes' layout configures the collaboration service instead, through GetBcrdtCollaborationService, once it is there.

await builder.Build().RunAsync();
