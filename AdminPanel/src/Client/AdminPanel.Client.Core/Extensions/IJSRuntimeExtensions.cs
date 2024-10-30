using System.Reflection;
using AdminPanel.Shared.Dtos.PushNotification;

namespace Microsoft.JSInterop;

public static partial class IJSRuntimeExtensions
{
    public static ValueTask<string> GetBrowserPlatform(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("App.getPlatform");
    }

    public static ValueTask<string> GetTimeZone(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("App.getTimeZone");
    }

    public static ValueTask ApplyBodyElementClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.InvokeVoidAsync("App.applyBodyElementClasses", cssClasses, cssVariables);
    }


    public static async ValueTask<DeviceInstallationDto> GetDeviceInstallation(this IJSRuntime jsRuntime, string vapidPublicKey)
    {
        return await jsRuntime.InvokeAsync<DeviceInstallationDto>("App.getDeviceInstallation", vapidPublicKey);
    }

    /// <summary>
    /// The return value would be false during pre-rendering
    /// </summary>
    public static bool IsInitialized(this IJSRuntime jsRuntime)
    {
        var type = jsRuntime.GetType();

        return type.Name switch
        {
            "UnsupportedJavaScriptRuntime" => false, // pre-rendering
            "RemoteJSRuntime" /* blazor server */ => (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)!,
            "WebViewJSRuntime" /* blazor hybrid */ => type.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(jsRuntime) is not null,
            _ => true // blazor wasm
        };
    }
}
