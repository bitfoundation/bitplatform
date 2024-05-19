﻿//+:cnd:noEmit
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
        return (bool)_IsInitializedProp.GetValue(jsRuntime)!;
    }
    private static PropertyInfo _IsInitializedProp = Assembly.Load("Microsoft.AspNetCore.Components.Server")!
                                                             .GetType("Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime")!
                                                             .GetProperty("IsInitialized")!;
}
