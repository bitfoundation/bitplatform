using Bit.ViewModel.Contracts;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;

namespace Bit.ViewModel.Implementations
{
    public class AppCenterTelemetryService : TelemetryServiceBase, ITelemetryService
    {
        private bool _isInited = false;

        private static AppCenterTelemetryService _current;

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

        public override void TrackEvent(string eventName, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Analytics.TrackEvent(eventName, properties);
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
#if !UWP
            properties = PopulateProperties(properties);
            Microsoft.AppCenter.Crashes.Crashes.TrackError(exception, properties);
#endif
            }
        }

        public override void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = properties ?? new Dictionary<string, string>();

                if (!properties.ContainsKey("Value"))
                    properties.Add("Value", value.ToString());

                TrackEvent($"Metric_{name}", properties);
            }
        }

        public override void TrackPageView(string name, TimeSpan duration, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = properties ?? new Dictionary<string, string>();

                if (!properties.ContainsKey("Name"))
                    properties.Add("Name", name);
                if (!properties.ContainsKey("Duration"))
                    properties.Add("Duration", duration.ToString());

                TrackEvent($"PageView_{name}", properties);
            }
        }

        public override void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = properties ?? new Dictionary<string, string>();

                if (!properties.ContainsKey("Name"))
                    properties.Add("Name", name);
                if (!properties.ContainsKey("Timestamp"))
                    properties.Add("Timestamp", startTime.ToString()); /*Based on Microsoft.ApplicationInsights.DataContracts.RequestTelemetry*/
                if (!properties.ContainsKey("Duration"))
                    properties.Add("Duration", responseCode);
                if (!properties.ContainsKey("Success"))
                    properties.Add("Success", success.ToString());
                if (!properties.ContainsKey("Url"))
                    properties.Add("Url", url.ToString());
                if (!properties.ContainsKey("httpMethod")) /*Based on Microsoft.ApplicationInsights.DataContracts.RequestTelemetry*/
                    properties.Add("httpMethod", httpMethod);

                TrackEvent($"Request_{name}", properties);
            }
        }

        public override void TrackTrace(string message, IDictionary<string, string> properties)
        {
            if (IsConfigured())
            {
                TrackEvent($"Trace_{message}", properties);
            }
        }
    }
}