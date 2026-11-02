using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Nothing of Bsqlite's here: Bit.Bsqlite.wasm and EF Core are not downloaded yet when this runs, and the calls would need
// them. The products page builds the services the first time it asks, through AppDatabase.

await builder.Build().RunAsync();
