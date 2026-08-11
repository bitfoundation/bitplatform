//+:cnd:noEmit
//#if (notification == true)
using Boilerplate.Shared.Features.PushNotification;
//#endif

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

        //#if (notification == true)
        public async ValueTask<PushNotificationSubscriptionDto> GetPushNotificationSubscription(string vapidPublicKey)
        {
            return await jsRuntime.InvokeAsync<PushNotificationSubscriptionDto>("App.getPushNotificationSubscription", vapidPublicKey);
        }
        //#endif

        /// <summary>
        /// The return value would be false during pre-rendering
        /// </summary>
        public bool IsInitialized()
        {
            return jsRuntime is not null && jsRuntime.IsRuntimeInvalid() is false;
        }

        /// <summary>
        /// Clears web browser / web view storages
        /// </summary>
        public async Task ClearWebStorages()
        {
            await jsRuntime.InvokeVoidAsync("App.clearWebStorages");
        }
    }
}
