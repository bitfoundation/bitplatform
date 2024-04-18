namespace Microsoft.JSInterop;

public static class IJSRuntimeExtensions
{
    public static ValueTask ApplyBodyElementClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.InvokeVoidAsync("App.applyBodyElementClasses", cssClasses, cssVariables);
    }

    public static ValueTask<string> GoogleRecaptchaGetResponse(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("grecaptcha.getResponse");
    }
}
