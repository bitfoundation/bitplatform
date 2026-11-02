using Bit.Bmotion.Samples.Lazy.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// No AddBitBmotionServices here: Bit.Bmotion.wasm is not downloaded yet when this runs, and the call would need it. The
// showcase's layout creates the services with ConfigureBmotionServices once it is there, and the page takes the one it
// animates with from GetBmotionService<T>().

var host = builder.Build();

// A visitor who arrives on the showcase has its prerendered page on screen already. The router renders nothing until
// OnNavigateAsync has finished, so a download started there would swap that page for an empty one until the assembly
// arrives; started here, before the app starts, it leaves the prerendered page up, and OnNavigateAsync then finds the
// assembly loaded and renders the same page at once.
var navigation = host.Services.GetRequiredService<NavigationManager>();
if (ShowcasePages.LazyLoadsBmotion && ShowcasePages.Contains(navigation.ToBaseRelativePath(navigation.Uri)))
{
    await host.Services.GetRequiredService<LazyAssemblyLoader>().LoadAssembliesAsync(["Bit.Bmotion.wasm"]);
}

await host.RunAsync();
