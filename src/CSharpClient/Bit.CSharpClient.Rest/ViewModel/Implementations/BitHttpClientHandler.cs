using Bit.ViewModel.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
#if Android
        protected override async Task WriteRequestContentToOutput(HttpRequestMessage request, Java.Net.HttpURLConnection httpConnection, CancellationToken cancellationToken)
        {
            // https://github.com/xamarin/xamarin-android/issues/4476

            var stream = await request.Content.ReadAsStreamAsync().ConfigureAwait(false);

            await stream.CopyToAsync(httpConnection.OutputStream, 4096, cancellationToken).ConfigureAwait(false);

            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
        }
#endif

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

            if (!request.Headers.Any(h => h.Key == "Client-Type"))
            {
                request.Headers.Add("Client-Type", "Xamarin");

                if (Device.Idiom != TargetIdiom.Unsupported)
                    request.Headers.Add("Client-Screen-Size", Device.Idiom == TargetIdiom.Phone ? "MobileAndPhablet" : "DesktopAndTablet");

                request.Headers.Add("Client-Date-Time", DefaultDateTimeProvider.Current.GetCurrentUtcDateTime().UtcDateTime.ToString("o", CultureInfo.InvariantCulture));

                request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

                request.Headers.Add("Bit-Client-Type", "CS-Client");
            }

            DateTimeOffset startDate = DateTimeOffset.Now;

#if Android // https://github.com/xamarin/xamarin-android/issues/3216
            try
#endif
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(response.ReasonPhrase) && response.Headers.TryGetValues("Reason-Phrase", out IEnumerable<string> reasonPhrases) && reasonPhrases.Any())
                {
                    response.ReasonPhrase = reasonPhrases.Single();
                }

                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    { "ReasonPhrase", response.ReasonPhrase }
                };

                if (response.Headers.TryGetValues("X-Correlation-ID", out IEnumerable<string> values) && values.Any())
                {
                    properties.Add("X-Correlation-ID", values.First());
                }

                TimeSpan duration = DateTimeOffset.Now - startDate;

                TelemetryServices.All().TrackRequest(request.RequestUri.LocalPath, startDate, duration, response.StatusCode.ToString(), response.IsSuccessStatusCode, request.RequestUri, request.Method.ToString(), properties);

                return response;
            }
#if Android
            catch (Java.Lang.Throwable exp)
            {
                throw exp switch
                {
                    Java.IO.IOException _ => new System.IO.IOException(exp.Message, exp),
                    _ => new Exception(exp.Message, exp),
                };
            }
#endif
        }

        public virtual IEnumerable<ITelemetryService> TelemetryServices { get; set; }
    }
}
