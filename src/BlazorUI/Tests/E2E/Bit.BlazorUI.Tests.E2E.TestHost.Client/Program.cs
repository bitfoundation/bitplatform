using Bit.BlazorUI;
using Bit.BlazorUI.Tests.E2E.TestHost.AI;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// The browser side of the E2E test host: the pages rendered in InteractiveWebAssembly mode get the same services the
// server registers for them in InteractiveServer mode, so every page runs unchanged in either mode.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBitBlazorUIServices();
builder.Services.AddBitBlazorUIExtrasServices();
// The AI package over the scripted transport of AI/E2EAiTransport.cs (how a page picks a reply is written there).
builder.Services.AddBitBlazorUIAIServices(E2EAiSetup.Configure);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
