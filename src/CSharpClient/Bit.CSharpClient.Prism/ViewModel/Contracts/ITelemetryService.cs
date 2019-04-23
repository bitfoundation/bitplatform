using System;
using System.Collections.Generic;

namespace Bit.ViewModel.Contracts
{
    public interface ITelemetryService
    {
        bool IsConfigured();

        IDictionary<string, string> PopulateProperties(IDictionary<string, string> initialProps);

        void TrackEvent(string eventName, IDictionary<string, string> properties = null);

        void TrackException(Exception exception, IDictionary<string, string> properties = null);

        void TrackMetric(string name, double value, IDictionary<string, string> properties = null);

        void TrackPageView(string name, TimeSpan duration, IDictionary<string, string> properties = null);

        void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string> properties = null);

        void TrackTrace(string message, IDictionary<string, string> properties);
    }
}
