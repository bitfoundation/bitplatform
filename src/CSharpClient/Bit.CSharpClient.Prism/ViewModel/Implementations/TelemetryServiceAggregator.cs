using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;
using System.Collections.Generic;

namespace Bit.ViewModel.Contracts
{
    public static class ITelemetryServiceExtensions
    {
        public static ITelemetryService All(this IEnumerable<ITelemetryService> telemetryServices)
        {
            return new TelemetryServiceAggregator(telemetryServices);
        }
    }
}

namespace Bit.ViewModel.Implementations
{
    public class TelemetryServiceAggregator : ITelemetryService
    {
        private readonly IEnumerable<ITelemetryService> _telemetryServices;

        public TelemetryServiceAggregator(IEnumerable<ITelemetryService> telemetryServices)
        {
            _telemetryServices = telemetryServices;
        }

        public virtual bool IsConfigured()
        {
            return true;
        }

        public virtual IDictionary<string, string> PopulateProperties(IDictionary<string, string> initialProps)
        {
            return initialProps;
        }

        public virtual void TrackEvent(string eventName, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackEvent(eventName, properties);
            }
        }

        public virtual void TrackException(Exception exception, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackException(exception, properties);
            }
        }

        public virtual void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackMetric(name, value, properties);
            }
        }

        public virtual void TrackPageView(string name, TimeSpan duration, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackPageView(name, duration, properties);
            }
        }

        public virtual void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string> properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackRequest(name, startTime, duration, responseCode, success, url, httpMethod, properties);
            }
        }

        public virtual void TrackTrace(string message, IDictionary<string, string> properties)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackTrace(message, properties);
            }
        }
    }
}
