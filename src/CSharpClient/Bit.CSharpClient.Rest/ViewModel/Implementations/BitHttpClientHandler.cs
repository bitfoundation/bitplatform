using Bit.ViewModel.Contracts;
using Prism.AppModel;
using Prism.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
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

        public virtual bool IsInDebugMode()
        {
            try
            {
                if (DeviceService.RuntimePlatform == RuntimePlatform.UWP)
                    return Assembly.GetEntryAssembly().GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
                else
                    return Debugger.IsAttached;
            }
            catch (Exception exp)
            {
                return Debugger.IsAttached;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ToDo:
            // Desired-Time-Zone

            if (!request.Headers.Any(h => h.Key == "Client-Type"))
            {
                request.Headers.Add("Client-Type", "Xamarin");

                if (DeviceService.Idiom != TargetIdiom.Unsupported)
                    request.Headers.Add("Client-Screen-Size", Device.Idiom == TargetIdiom.Phone ? "MobileAndPhablet" : "DesktopAndTablet");

                request.Headers.Add("Client-Date-Time", DateTimeProvider.GetCurrentUtcDateTime().UtcDateTime.ToString("o", CultureInfo.InvariantCulture));

                request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

                request.Headers.Add("Bit-Client-Type", "CS-Client");

                request.Headers.Add("Client-App-Version", AppInfo.VersionString);

                request.Headers.Add("Client-Culture", CultureInfo.CurrentUICulture.Name);

                request.Headers.Add("System-Language", CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName);

                request.Headers.Add("Client-Sys-Language", CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName);

                try
                {
                    request.Headers.Add("Client-Route", BitApplication.Current.NavigationService.CurrentPageNavService.GetNavigationUriPath());
                }
                catch { }

                request.Headers.Add("Client-Platform", DeviceInfo.Platform.ToString());

                request.Headers.Add("Current-Time-Zone", TimeZoneInfo.Local.Id);

                if (DeviceService.RuntimePlatform != RuntimePlatform.UWP)
                    request.Headers.Add("Client-Theme", AppInfo.RequestedTheme.ToString());

                request.Headers.Add("Client-Debug-Mode", IsInDebugMode().ToString(CultureInfo.InvariantCulture));
            }

            DateTimeOffset startDate = DateTimeProvider.GetCurrentUtcDateTime();

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

        public virtual IDateTimeProvider DateTimeProvider { get; set; }

        public virtual IDeviceService DeviceService { get; set; }
    }
}
