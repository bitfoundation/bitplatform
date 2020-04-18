using Bit.View;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Exceptions;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Bit.ViewModel.Implementations
{
    public class AppCenterTelemetryService : TelemetryServiceBase, ITelemetryService
    {
        private bool _isInited = false;

        private static AppCenterTelemetryService _current = default!;

        public static AppCenterTelemetryService Current
        {
            get => _current ?? (_current = new AppCenterTelemetryService());
            set => _current = value;
        }

#if UWP || Android || iOS
        public virtual void Init(string appSecret, params Type[] services)
        {
            AppCenter.Start(appSecret, services);
            _isInited = true;
        }
#endif

        public override bool IsConfigured()
        {
            return _isInited;
        }

        public override void TrackEvent(string eventName, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Analytics.TrackEvent(eventName, properties);
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Crashes.TrackError(exception, properties);
            }
        }

        public override void TrackMetric(string name, double value, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties ??= new Dictionary<string, string?>();

                if (!properties.ContainsKey("Value"))
                    properties.Add("Value", value.ToString(CultureInfo.InvariantCulture));

                TrackEvent($"Metric_{name}", properties);
            }
        }

        public override void TrackPageView(string name, TimeSpan duration, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties ??= new Dictionary<string, string?>();

                if (!properties.ContainsKey("Name"))
                    properties.Add("Name", name);
                if (!properties.ContainsKey("Duration"))
                    properties.Add("Duration", duration.ToString());

                TrackEvent($"PageView_{name}", properties);
            }
        }

        public override void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties ??= new Dictionary<string, string?>();

                if (!properties.ContainsKey("Name"))
                    properties.Add("Name", name);
                if (!properties.ContainsKey("Timestamp"))
                    properties.Add("Timestamp", startTime.ToString(CultureInfo.InvariantCulture)); /*Based on Microsoft.ApplicationInsights.DataContracts.RequestTelemetry*/
                if (!properties.ContainsKey("Duration"))
                    properties.Add("Duration", duration.ToString());
                if (!properties.ContainsKey("Success"))
                    properties.Add("Success", success.ToString(CultureInfo.InvariantCulture));
                if (!properties.ContainsKey("Url") && url != null)
                    properties.Add("Url", url.ToString());
                if (!properties.ContainsKey("httpMethod")) /*Based on Microsoft.ApplicationInsights.DataContracts.RequestTelemetry*/
                    properties.Add("httpMethod", httpMethod);

                TrackEvent($"Request_{name}", properties);
            }
        }

        public override void TrackTrace(string message, IDictionary<string, string?>? properties)
        {
            if (IsConfigured())
            {
                TrackEvent($"Trace_{message}", properties);
            }
        }

        public override void SetUserId(string? userId)
        {
            if (IsConfigured())
            {
                AppCenter.SetUserId(userId);
            }
        }

        public override async void LogPreviousSessionCrashIfAny()
        {
            if (IsConfigured())
            {
                try
                {
                    if (await Crashes.HasCrashedInLastSessionAsync().ConfigureAwait(false))
                    {
                        var crashReport = await Crashes.GetLastSessionCrashReportAsync().ConfigureAwait(false);

                        var hasReceivedMemoryWarningInLastSession = await Crashes.HasReceivedMemoryWarningInLastSessionAsync().ConfigureAwait(false);

                        var exp = new CrashReportException { };

                        var items = new Dictionary<string, string?>
                        {
                            { "CrashReportId", crashReport?.Id },
                            { "HasReceivedMemoryWarningInLastSession", hasReceivedMemoryWarningInLastSession.ToString(CultureInfo.InvariantCulture) },
                            { "VersionHistory", string.Join(",", VersionTracking.VersionHistory.OrderByDescending(vh => vh)) },
                            { "Version", string.Join(",", VersionTracking.CurrentVersion) },
                            { "XamarinFormsVersion", typeof(Binding).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version },
                            { "BitVersion", typeof(BitCSharpClientControls).Assembly.GetName().Version!.ToString() },
                            { "CurrentUICulture", CultureInfo.CurrentUICulture.Name }
                        };

                        Crashes.TrackError(exp, items);
                    }
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }
        }
    }
}