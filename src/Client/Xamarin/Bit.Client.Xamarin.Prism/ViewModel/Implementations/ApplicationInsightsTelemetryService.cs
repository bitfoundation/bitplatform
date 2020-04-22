using Bit.Core.Contracts;
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
        private TelemetryClient? _client;
        private bool _isInited = false;
#if !UWP
        private TelemetryConfiguration? _configuration;
#endif
        private static ApplicationInsightsTelemetryService _current = default!;

        public static ApplicationInsightsTelemetryService Current
        {
            get => _current ?? (_current = new ApplicationInsightsTelemetryService());
            set => _current = value;
        }

        public virtual void Init(string instrumentationKey)
        {
#if Android
            ((AppDomainSetup)AppDomain.CurrentDomain.GetType().GetProperty("SetupInformationNoCopy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(AppDomain.CurrentDomain))
                .ApplicationBase = "/"; // workaround for app insight which tries to read app config file.
#endif

#if UWP
            WindowsAppInitializer.InitializeAsync(instrumentationKey: instrumentationKey);
#else
            _configuration = TelemetryConfiguration.CreateDefault();
            _configuration.InstrumentationKey = instrumentationKey;
#endif
            _isInited = true;
        }

#if UWP
        protected virtual TelemetryClient Client => _client ?? (_client = new TelemetryClient(TelemetryConfiguration.Active));
#else
        protected virtual TelemetryClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new TelemetryClient(_configuration)
                    {
                        InstrumentationKey = _configuration!.InstrumentationKey
                    };

#if XamarinEssentials
                    _client.Context.Device.Model = Xamarin.Essentials.DeviceInfo.Model;
#endif
                    _client.Context.Device.OperatingSystem = Xamarin.Forms.Device.RuntimePlatform;
                }
                return _client;
            }
        }
#endif

        public override bool IsConfigured()
        {
#if UWP
            return _isInited && TelemetryConfiguration.Active != null;
#else
            return _isInited && _configuration != null;
#endif
        }

        public override void TrackEvent(string eventName, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackEvent(eventName, properties);
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackException(exception, properties, metrics: null);
            }
        }

        public override void TrackMetric(string name, double value, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackMetric(name, value, properties);
            }
        }

        public override void TrackPageView(string name, TimeSpan duration, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);

                PageViewTelemetry pageViewTelemetry = new PageViewTelemetry(name)
                {
                    Duration = duration
                };

                foreach (KeyValuePair<string, string?> prp in properties)
                {
                    pageViewTelemetry.Properties.Add(prp.Key, prp.Value);
                }

                Client.TrackPageView(pageViewTelemetry);
            }
        }

        public override void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);

                RequestTelemetry requestTelemetry = new RequestTelemetry(name, startTime, duration, responseCode, success)
                {
                    HttpMethod = httpMethod,
                    Url = url
                };

                foreach (KeyValuePair<string, string?> prp in properties)
                {
                    requestTelemetry.Properties.Add(prp.Key, prp.Value);
                }

                if (properties.TryGetValue("X-Correlation-ID", out string? xCorrelationId))
                {
                    requestTelemetry.Id = xCorrelationId;
                }

                Client.TrackRequest(requestTelemetry);
            }
        }

        public override void TrackTrace(string message, IDictionary<string, string?>? properties)
        {
            if (IsConfigured())
            {
                properties = PopulateProperties(properties);
                Client.TrackTrace(message, properties);
            }
        }

        public override void SetUserId(string? userId)
        {
            if (IsConfigured())
            {
#if UWP
                Client.Context.User.Id = Client.Context.User.AccountId = userId;
#else
                Client.Context.User.Id = Client.Context.User.AccountId = Client.Context.User.AuthenticatedUserId = userId;
#endif
            }

        }
    }
}