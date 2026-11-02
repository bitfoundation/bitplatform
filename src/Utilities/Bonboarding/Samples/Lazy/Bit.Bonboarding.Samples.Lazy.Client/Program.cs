using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBonboardingServices here: Bit.Bonboarding.wasm is not downloaded yet when this runs, and the call would need
// it. The Bit.Bonboarding.LazyLoad switch (the project file) has the tour and BonboardingRoot create the services
// themselves, and the admin pages take the same IBonboardingService from GetBonboardingService().

await builder.Build().RunAsync();
