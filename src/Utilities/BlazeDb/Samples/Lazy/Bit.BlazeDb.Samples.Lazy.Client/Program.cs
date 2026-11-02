using Bit.BlazeDb.Samples.Lazy.Client.Data;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// BlazeDb registers no services. The app's own database service is registered here, before Bit.BlazeDb.wasm is
// downloaded: registering it lays the type out, and AppDatabase keeps the engine's types out of its own fields, so it
// names nothing the browser does not have yet. The todos page opens the database through it.
builder.Services.AddSingleton<AppDatabase>();

await builder.Build().RunAsync();
