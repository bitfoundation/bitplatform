// The WebAssembly half of the demo: the same service registration as the server, through AddDemoServices.
// Every database this site opens lives here, in the browser tab.

using Bit.Bsqlite.Demo.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDemoServices(builder.HostEnvironment.BaseAddress);

TaskScheduler.UnobservedTaskException += (_, args) => SampleLog.Exception(args.Exception);
AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    if (args.ExceptionObject is Exception exception)
    {
        SampleLog.Exception(exception);
    }
    else
    {
        Console.Error.WriteLine($"[Bsqlite demo] Unhandled: {args.ExceptionObject}");
    }
};

// No RootComponents are registered on purpose: the host page drives the app through blazor.web.js
// render-mode markers (<Routes @rendermode="InteractiveWebAssembly" />).
var host = builder.Build();

SampleLog.Logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Bit.Bsqlite.Demo");

await host.RunAsync();
