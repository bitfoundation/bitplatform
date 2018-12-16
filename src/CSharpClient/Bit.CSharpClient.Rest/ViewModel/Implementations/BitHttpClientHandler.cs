using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.ViewModel.Implementations
{
    public class BitHttpClientHandler :
#if Android
        Xamarin.Android.Net.AndroidClientHandler
#elif iOS
        NSUrlSessionHandler
#else
        HttpClientHandler
#endif
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ToDo:
            // Current-Time-Zone
            // Desired-Time-Zone
            // Client-App-Version
            // Client-Culture
            // Client-Route
            // Client-Theme
            // Client-Debug-Mode
            // System-Language
            // Client-Sys-Language
            // Client-Platform

            // ToDo: Use IDeviceService & IDateTimeProvider

            request.Headers.Add("Client-Type", "Xamarin");

            if (Device.Idiom != TargetIdiom.Unsupported)
                request.Headers.Add("Client-Screen-Size", Device.Idiom == TargetIdiom.Phone ? "MobileAndPhablet" : "DesktopAndTablet");

            request.Headers.Add("Client-Date-Time", DefaultDateTimeProvider.Current.GetCurrentUtcDateTime().UtcDateTime.ToString("o"));

            request.Headers.Add("X-CorrelationId", Guid.NewGuid().ToString());

            request.Headers.Add("Bit-Client-Type", "CS-Client");

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(response.ReasonPhrase) && response.Headers.TryGetValues("Reason-Phrase", out IEnumerable<string> reasonPhrases) && reasonPhrases.Any())
            {
                response.ReasonPhrase = reasonPhrases.Single();
            }

            return response;
        }
    }
}
