using Bit.BlazorUI;
using Bit.BlazorUI.Tests.Trimming.LazyAi;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// The core's services alone. AddBitBlazorUIAIServices is a method of Bit.BlazorUI.AI, so calling it here would load the
// assembly at startup: the chat page hands BitAiChat its session instead (ChatPage.razor).
builder.Services.AddBitBlazorUIServices();

await builder.Build().RunAsync();
