using Bit.BlazorUI;
using Bit.BlazorUI.Tests.Consumer;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBitBlazorUIServices();
builder.Services.AddBitBlazorUIExtrasServices();
// The AI package's services, and the transport its chat streams the replies from: one of the app's own.
builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<EchoTransport>());

// The packaged presets register themselves once the Extras assembly loads; an app that never names one of its types
// registers them by hand, through the plain type.
BitExtraThemeRegistration.Register();

await builder.Build().RunAsync();
