using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Nothing of Blow's to register, here or in any app: the canvas takes only IJSRuntime from the container and imports
// its script module itself. So nothing here needs Bit.Blow.wasm, which is not downloaded yet.

await builder.Build().RunAsync();
