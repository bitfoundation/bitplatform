//+:cnd:noEmit
namespace Microsoft.JSInterop;

public static partial class IJSRuntimeExtensions
{
    extension(IJSRuntime jsRuntime)
    {
        public ValueTask<string> GetTimeZone()
        {
            return jsRuntime.InvokeAsync<string>("App.getTimeZone");
        }

        //#if (captcha == "reCaptcha")
        public ValueTask<string> GoogleRecaptchaGetResponse()
        {
            return jsRuntime.InvokeAsync<string>("grecaptcha.getResponse");
        }

        public ValueTask<string> GoogleRecaptchaReset()
        {
            return jsRuntime.InvokeAsync<string>("grecaptcha.reset");
        }
        //#endif

        /// <summary>
        /// The return value would be false during pre-rendering
        /// </summary>
        public bool IsInitialized()
        {
            return jsRuntime is not null && jsRuntime.IsRuntimeInvalid() is false;
        }
    }
}
