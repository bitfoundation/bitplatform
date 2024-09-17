//+:cnd:noEmit
using System.Reflection;

namespace Microsoft.JSInterop;

public static partial class IJSRuntimeExtensions
{
    public static ValueTask<string> GetBrowserPlatform(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("App.getPlatform");
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

    public static bool IsInPrerenderSession(this IJSRuntime jsRuntime)
    {
        var type = jsRuntime.GetType();

        var jsRuntimeIsInitialized = type.Name switch
        {
            "UnsupportedJavaScriptRuntime" => false, // pre-rendering
            "RemoteJSRuntime" => (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)!, // blazor server
            _ => true // blazor wasm / hybrid
        };

        return jsRuntimeIsInitialized is false;
    }
}
