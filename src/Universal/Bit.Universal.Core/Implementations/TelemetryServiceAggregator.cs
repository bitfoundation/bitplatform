﻿using Bit.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Core.Implementations
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

        public void LogPreviousSessionCrashIfAny()
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.LogPreviousSessionCrashIfAny();
            }
        }

        public virtual IDictionary<string, string?> PopulateProperties(IDictionary<string, string?>? initialProps)
        {
            return initialProps ?? new Dictionary<string, string?>();
        }

        public void SetUserId(string? userId)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.SetUserId(userId);
            }
        }

        public virtual void TrackEvent(string eventName, IDictionary<string, string?>? properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackEvent(eventName, properties);
            }
        }

        public virtual void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackException(exception, properties);
            }
        }

        public virtual void TrackMetric(string name, double value, IDictionary<string, string?>? properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackMetric(name, value, properties);
            }
        }

        public virtual void TrackPageView(string name, TimeSpan duration, IDictionary<string, string?>? properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackPageView(name, duration, properties);
            }
        }

        public virtual void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string?>? properties = null)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackRequest(name, startTime, duration, responseCode, success, url, httpMethod, properties);
            }
        }

        public virtual void TrackTrace(string message, IDictionary<string, string?>? properties)
        {
            foreach (ITelemetryService telemetryService in _telemetryServices)
            {
                telemetryService.TrackTrace(message, properties);
            }
        }

        public virtual void TrackEvent(string eventName, (string key, string? value)[] properties)
        {
            TrackEvent(eventName, properties.ToDictionary(item => item.key, item => item.value));
        }

        public virtual void TrackException(Exception exception, (string key, string? value)[] properties)
        {
            TrackException(exception, properties.ToDictionary(item => item.key, item => item.value));
        }

        public virtual void TrackMetric(string name, double value, (string key, string? value)[] properties)
        {
            TrackMetric(name, value, properties.ToDictionary(item => item.key, item => item.value));
        }

        public virtual void TrackPageView(string name, TimeSpan duration, (string key, string? value)[] properties)
        {
            TrackPageView(name, duration, properties.ToDictionary(item => item.key, item => item.value));
        }

        public virtual void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, (string key, string? value)[] properties)
        {
            TrackRequest(name, startTime, duration, responseCode, success, url, httpMethod, properties.ToDictionary(item => item.key, item => item.value));
        }

        public virtual void TrackTrace(string message, (string key, string? value)[] properties)
        {
            TrackTrace(message, properties.ToDictionary(item => item.key, item => item.value));
        }

        public virtual async Task<bool> DidCrashOnPreviousExecution()
        {
            return false;
        }
    }
}
