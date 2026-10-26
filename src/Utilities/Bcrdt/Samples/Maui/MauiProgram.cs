using Microsoft.Extensions.Logging;

namespace Bit.Bcrdt.Samples.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // One app, one DI container and one replica: the BlazorWebView renders every component in this
        // process, so the collaboration service is registered here alone. Local-first: every document is kept
        // in the app's data directory and stays fully editable with no server at all.
        //
        // To edit alongside the browser tabs of a web sample, give it that sample's hub as well (from the
        // Android emulator the host machine is 10.0.2.2, and plain HTTP has to be allowed there):
        //     .UseTransport(new Bit.Bcrdt.SignalR.BcrdtSignalRTransportFactory("http://10.0.2.2:5191/hub/bcrdt"))
        builder.Services.AddCoreServices(collaboration => collaboration
            .UseStore(new BcrdtFileSystemDocumentStore(Path.Combine(FileSystem.AppDataDirectory, "documents"))));

        return builder.Build();
    }
}
