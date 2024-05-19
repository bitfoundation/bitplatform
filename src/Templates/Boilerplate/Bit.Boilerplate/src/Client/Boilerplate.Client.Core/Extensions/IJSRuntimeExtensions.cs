//+:cnd:noEmit
using System.Reflection;

namespace Microsoft.JSInterop;

public static class IJSRuntimeExtensions
{
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

    public static bool IsInitialized(this IJSRuntime jsRuntime)
    {
        var type = jsRuntime.GetType();

        if (type.Name is not "RemoteJSRuntime") return true; // Blazor WASM/Hybrid

        return (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)!;
    }
}
