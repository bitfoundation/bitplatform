#if UWP
using Bit.ViewModel.Contracts;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;

namespace Bit.ViewModel.Implementations
{
    public class ApplicationInsightsTelemetryService : TelemetryServiceBase, ITelemetryService
    {
        private TelemetryClient _client;
        private bool _isInited = false;
        private static ApplicationInsightsTelemetryService _current;

        public static ApplicationInsightsTelemetryService Current
        {
            get => _current ?? (_current = new ApplicationInsightsTelemetryService());
            set => _current = value;
        }

        public virtual void Init(string instrumentationKey)
        {
            WindowsAppInitializer.InitializeAsync(instrumentationKey: instrumentationKey);
            _isInited = true;
        }

        protected virtual TelemetryClient Client => _client ?? (_client = new TelemetryClient(TelemetryConfiguration.Active));

        public override bool IsConfigured()
        {
            return _isInited && TelemetryConfiguration.Active != null;
        }

        public override void TrackEvent(string eventName, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackEvent(eventName, properties);
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackException(exception, properties, metrics: null);
            }
        }

        public override void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackMetric(name, value, properties);
            }
        }

        public override void TrackPageView(string name, TimeSpan duration, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);

                PageViewTelemetry pageViewTelemetry = new PageViewTelemetry(name)
                {
                    Duration = duration
                };

                foreach (KeyValuePair<string, string> prp in properties)
                {
                    pageViewTelemetry.Properties.Add(prp.Key, prp.Value);
                }

                Client.TrackPageView(pageViewTelemetry);
            }
        }

        public override void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string> properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);

                RequestTelemetry requestTelemetry = new RequestTelemetry(name, startTime, duration, responseCode, success)
                {
                    HttpMethod = httpMethod,
                    Url = url
                };

                foreach (KeyValuePair<string, string> prp in properties)
                {
                    requestTelemetry.Properties.Add(prp.Key, prp.Value);
                }

                Client.TrackRequest(requestTelemetry);
            }
        }

        public override void TrackTrace(string message, IDictionary<string, string> properties)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackTrace(message, properties);
            }
        }
    }
}
#endif