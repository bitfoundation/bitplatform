using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Nothing of Bink's to register, here or in any app: the canvas takes only IJSRuntime and NavigationManager from the
// container and imports its browser module itself. So nothing here needs Bit.Bink.wasm, which is not downloaded yet.

await builder.Build().RunAsync();
