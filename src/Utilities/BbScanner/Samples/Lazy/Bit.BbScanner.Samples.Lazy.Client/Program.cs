using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Nothing of BbScanner here: it has no services to register, and Bit.BbScanner.wasm is not downloaded yet when this
// runs. The scanner imports its JavaScript module itself the first time it renders.

await builder.Build().RunAsync();
