using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBaletteServices here: Bit.Balette.wasm is not downloaded yet when this runs, and the call would need it. The
// palette and its trigger create their services themselves, and the admin page registers its command with the same
// palette through GetBaletteService().

// The palette logs once that the services are not registered, which is expected here. The filter names the logger's
// category as a string, so it silences that warning without referencing Balette's types.
builder.Logging.AddFilter("Bit.Balette.BitBalette", LogLevel.None);

await builder.Build().RunAsync();
