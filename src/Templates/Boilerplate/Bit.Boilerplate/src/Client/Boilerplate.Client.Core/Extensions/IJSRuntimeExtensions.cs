//+:cnd:noEmit
using System.Reflection;
//#if (notification == true)
using Boilerplate.Shared.Dtos.PushNotification;
//#endif

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

    //#if (captcha == "reCaptcha")
    public static ValueTask<string> GoogleRecaptchaGetResponse(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("grecaptcha.getResponse");
    }

    public static ValueTask<string> GoogleRecaptchaReset(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("grecaptcha.reset");
    }
    //#endif

    //#if (notification == true)
    public static async ValueTask<DeviceInstallationDto> GetDeviceInstallation(this IJSRuntime jsRuntime, string vapidPublicKey)
    {
        return await jsRuntime.InvokeAsync<DeviceInstallationDto>("App.getDeviceInstallation", vapidPublicKey);
    }
    //#endif

    /// <summary>
    /// The return value would be false during pre-rendering
    /// </summary>
    public static bool IsInitialized(this IJSRuntime jsRuntime)
    {
        if (jsRuntime is null)
            return false;

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
