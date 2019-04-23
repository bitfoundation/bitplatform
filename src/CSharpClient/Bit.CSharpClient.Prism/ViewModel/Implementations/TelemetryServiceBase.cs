using Bit.ViewModel.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xamarin.Essentials;

namespace Bit.ViewModel.Implementations
{
    public abstract class TelemetryServiceBase : ITelemetryService
    {
        public virtual IMessageReceiver MessageReceiver { get; set; }

        public virtual IDictionary<string, string> PopulateProperties(IDictionary<string, string> initialProps)
        {
            initialProps = initialProps ?? new Dictionary<string, string> { };

            if (!initialProps.ContainsKey("MemoryUsage"))
                initialProps.Add("MemoryUsage", $"{GC.GetTotalMemory(forceFullCollection: false):#,##0} bytes");

            if (!initialProps.ContainsKey("CurrentUICulture"))
                initialProps.Add("CurrentUICulture", CultureInfo.CurrentUICulture.Name);

            if (!initialProps.ContainsKey("VersionHistory"))
                initialProps.Add("VersionHistory", string.Join(",", VersionTracking.VersionHistory));

            if (!initialProps.ContainsKey("IsFirstLaunchEver"))
                initialProps.Add("IsFirstLaunchEver", VersionTracking.IsFirstLaunchEver.ToString());

            if (!initialProps.ContainsKey("IsFirstLaunchForCurrentVersion"))
                initialProps.Add("IsFirstLaunchForCurrentVersion", VersionTracking.IsFirstLaunchForCurrentVersion.ToString());

            if (!initialProps.ContainsKey("NetworkAccess"))
                initialProps.Add("NetworkAccess", Connectivity.NetworkAccess.ToString());

            if (MessageReceiver != null && !initialProps.ContainsKey("MessageReceiverWasConnected"))
                initialProps.Add("MessageReceiverWasConnected", MessageReceiver.IsConnected.ToString());

            return initialProps;
        }

        public abstract bool IsConfigured();

        public abstract void TrackEvent(string eventName, IDictionary<string, string> properties = null);

        public abstract void TrackException(Exception exception, IDictionary<string, string> properties = null);

        public abstract void TrackMetric(string name, double value, IDictionary<string, string> properties = null);

        public abstract void TrackPageView(string name, TimeSpan duration, IDictionary<string, string> properties = null);

        public abstract void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string> properties = null);

        public abstract void TrackTrace(string message, IDictionary<string, string> properties);
    }
}
