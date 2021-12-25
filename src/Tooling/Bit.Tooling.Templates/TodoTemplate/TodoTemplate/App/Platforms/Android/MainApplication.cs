using Android.App;
using Android.Runtime;
using TodoTemplate.App.Platforms.Android.Handlers;

[assembly: UsesPermission(Android.Manifest.Permission.ReadContacts)]

#if DEBUG
[assembly: UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]
#endif

namespace TodoTemplate.App.Platforms.Android;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram
        .CreateMauiAppBuilder()
        .ConfigureMauiHandlers(delegate (IMauiHandlersCollection handlers)
        {
            var descriptorToRemove = handlers.FirstOrDefault(d => d.ServiceType == typeof(IBlazorWebView));
            handlers.Remove(descriptorToRemove);

            handlers.AddHandler<IBlazorWebView, CustomBlazorWebViewHandler>();
        })
        .Build();
}