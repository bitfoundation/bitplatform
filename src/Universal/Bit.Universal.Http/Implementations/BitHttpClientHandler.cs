using Bit.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials.Interfaces;

namespace Bit.Http.Implementations
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
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (httpConnection == null)
                throw new ArgumentNullException(nameof(httpConnection));

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
#if UWP
                return Assembly.GetEntryAssembly()!.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
#else
                return Debugger.IsAttached;
#endif
            }
            catch
            {
#if UWP
                return Debugger.IsAttached;
#else
                return false;
#endif
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.RequestUri.Segments?.Last() == "$ref")
                return new HttpResponseMessage(HttpStatusCode.OK);

            // ToDo:
            // Desired-Time-Zone

            Dictionary<string, string?> properties = new Dictionary<string, string?>
            {

            };

            Guid xCorrelationID;

            if (!request.Headers.Any(h => h.Key == "Client-Type"))
            {
                request.Headers.Add("Client-Type", "Xamarin");

                if (DeviceInfo.Idiom != Xamarin.Essentials.DeviceIdiom.Unknown)
                    request.Headers.Add("Client-Screen-Size", DeviceInfo.Idiom == Xamarin.Essentials.DeviceIdiom.Phone ? "MobileAndPhablet" : "DesktopAndTablet");

                request.Headers.Add("Client-Date-Time", DateTimeProvider.GetCurrentUtcDateTime().UtcDateTime.ToString("o", CultureInfo.InvariantCulture));

                xCorrelationID = Guid.NewGuid();

                request.Headers.Add("X-Correlation-ID", xCorrelationID.ToString());

                request.Headers.Add("Bit-Client-Type", "CS-Client");

                request.Headers.Add("Client-App-Version", AppInfo.VersionString);

                request.Headers.Add("Client-Culture", CultureInfo.CurrentUICulture.Name);

                request.Headers.Add("System-Language", CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName);

                request.Headers.Add("Client-Sys-Language", CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName);

                try
                {
                    request.Headers.Add("Client-Route", DependencyDelegates.Current.GetNavigationUriPath?.Invoke());
                }
                catch { }

                request.Headers.Add("Client-Platform", DeviceInfo.Platform.ToString());

                request.Headers.Add("Current-Time-Zone", TimeZoneInfo.Local.Id);

#if !UWP
                request.Headers.Add("Client-Theme", AppInfo.RequestedTheme.ToString());
#endif

                request.Headers.Add("Client-Debug-Mode", IsInDebugMode().ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                xCorrelationID = Guid.Parse(request.Headers.ExtendedSingle("Finding X-Correlation-ID", h => h.Key == "X-Correlation-ID").Value.ExtendedSingle("Getting X-Correlation-ID value"));
            }

            DateTimeOffset startDate = DateTimeProvider.GetCurrentUtcDateTime();

            HttpResponseMessage? response = null;

            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                return response;
            }
#if Android
            catch (Java.Lang.Throwable exp) // https://github.com/xamarin/xamarin-android/issues/3216
            {
                throw exp switch
                {
                    Java.IO.IOException _ => new IOException(exp.Message, exp),
                    _ => new Exception(exp.Message, exp),
                };
            }
#endif
            catch (HttpRequestException exp)
            {
                properties.Add("RequestException", exp.ToString());
                throw;
            }
            finally
            {
                if (response != null)
                {
                    if (response.Headers.TryGetValues("Reason-Phrase", out IEnumerable<string> reasonPhrases) && reasonPhrases.Any())
                    {
                        response.ReasonPhrase = reasonPhrases.Single();
                    }
                }

                properties.Add("ReasonPhrase", response?.ReasonPhrase ?? "UnknownError");

                properties.Add("X-Correlation-ID", xCorrelationID.ToString());

                TimeSpan duration = DateTimeOffset.Now - startDate;

                TelemetryServices.All().TrackRequest(request.RequestUri.LocalPath, startDate, duration, response?.StatusCode.ToString() ?? "UnknownStatusCode", response?.IsSuccessStatusCode ?? false, request.RequestUri, request.Method.ToString(), properties);
            }
        }

        public virtual IEnumerable<ITelemetryService> TelemetryServices { get; set; } = default!;

        public virtual IDateTimeProvider DateTimeProvider { get; set; } = default!;

        public virtual IDeviceInfo DeviceInfo { get; set; } = default!;

        public virtual IAppInfo AppInfo { get; set; } = default!;
    }
}
