using Bit.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.Core.Implementations
{
    public abstract class TelemetryServiceBase : ITelemetryService
    {
        public IMessageReceiver? MessageReceiver { get; set; }

        public virtual IDictionary<string, string?> PopulateProperties(IDictionary<string, string?>? initialProps)
        {
            initialProps ??= new Dictionary<string, string?> { };

            if (!initialProps.ContainsKey("MemoryUsage"))
                initialProps.Add("MemoryUsage", $"{GC.GetTotalMemory(forceFullCollection: false):#,##0} bytes");

            if (!initialProps.ContainsKey("CurrentUICulture"))
                initialProps.Add("CurrentUICulture", CultureInfo.CurrentUICulture.Name);

#if Xamarin
            if (!initialProps.ContainsKey("NetworkAccess"))
                initialProps.Add("NetworkAccess", Xamarin.Essentials.Connectivity.NetworkAccess.ToString());

            if (!initialProps.ContainsKey("VersionHistory"))
                initialProps.Add("VersionHistory", string.Join(",", Xamarin.Essentials.VersionTracking.VersionHistory.OrderByDescending(vh => vh)));

            if (!initialProps.ContainsKey("Version"))
                initialProps.Add("Version", string.Join(",", Xamarin.Essentials.VersionTracking.CurrentVersion));

            if (!initialProps.ContainsKey("IsFirstLaunchEver"))
                initialProps.Add("IsFirstLaunchEver", Xamarin.Essentials.VersionTracking.IsFirstLaunchEver.ToString(CultureInfo.InvariantCulture));

            if (!initialProps.ContainsKey("IsFirstLaunchForCurrentVersion"))
                initialProps.Add("IsFirstLaunchForCurrentVersion", Xamarin.Essentials.VersionTracking.IsFirstLaunchForCurrentVersion.ToString(CultureInfo.InvariantCulture));
#elif Maui
            if (!initialProps.ContainsKey("NetworkAccess"))
                initialProps.Add("NetworkAccess", Microsoft.Maui.Networking.Connectivity.NetworkAccess.ToString());

            if (!initialProps.ContainsKey("VersionHistory"))
                initialProps.Add("VersionHistory", string.Join(",", Microsoft.Maui.ApplicationModel.VersionTracking.VersionHistory.OrderByDescending(vh => vh)));

            if (!initialProps.ContainsKey("Version"))
                initialProps.Add("Version", string.Join(",", Microsoft.Maui.ApplicationModel.VersionTracking.CurrentVersion));

            if (!initialProps.ContainsKey("IsFirstLaunchEver"))
                initialProps.Add("IsFirstLaunchEver", Microsoft.Maui.ApplicationModel.VersionTracking.IsFirstLaunchEver.ToString(CultureInfo.InvariantCulture));

            if (!initialProps.ContainsKey("IsFirstLaunchForCurrentVersion"))
                initialProps.Add("IsFirstLaunchForCurrentVersion", Microsoft.Maui.ApplicationModel.VersionTracking.IsFirstLaunchForCurrentVersion.ToString(CultureInfo.InvariantCulture));
#endif

            if (MessageReceiver != null && !initialProps.ContainsKey("MessageReceiverWasConnected"))
                initialProps.Add("MessageReceiverWasConnected", MessageReceiver.IsConnected.ToString(CultureInfo.InvariantCulture));

            if (!initialProps.ContainsKey("XamarinFormsVersion"))
            {
                Type? bindingType = Type.GetType("Xamarin.Forms.Binding, Xamarin.Forms.Core", throwOnError: false);
                if (bindingType != null)
                    initialProps.Add("XamarinFormsVersion", bindingType.Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);
            }

            if (!initialProps.ContainsKey("BitVersion"))
                initialProps.Add("BitVersion", typeof(TelemetryServiceBase).Assembly.GetName().Version!.ToString());

#if (Android || iOS) && !Maui
            if (!initialProps.ContainsKey("Mono"))
                initialProps.Add("Mono", Mono.Runtime.GetDisplayName());
#endif

            return initialProps;
        }

        public abstract bool IsConfigured();

        public virtual void TrackEvent(string eventName, (string key, string? value)[] properties)
        {
            TrackEvent(eventName, properties.ToDictionary(item => item.key, item => item.value));
        }

        public abstract void TrackEvent(string eventName, IDictionary<string, string?>? properties = null);

        public virtual void TrackException(Exception exception, (string key, string? value)[] properties)
        {
            TrackException(exception, properties.ToDictionary(item => item.key, item => item.value));
        }

        public abstract void TrackException(Exception exception, IDictionary<string, string?>? properties = null);

        public virtual void TrackMetric(string name, double value, (string key, string? value)[] properties)
        {
            TrackMetric(name, value, properties.ToDictionary(item => item.key, item => item.value));
        }

        public abstract void TrackMetric(string name, double value, IDictionary<string, string?>? properties = null);

        public virtual void TrackPageView(string name, TimeSpan duration, (string key, string? value)[] properties)
        {
            TrackPageView(name, duration, properties.ToDictionary(item => item.key, item => item.value));
        }

        public abstract void TrackPageView(string name, TimeSpan duration, IDictionary<string, string?>? properties = null);

        public virtual void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, (string key, string? value)[] properties)
        {
            TrackRequest(name, startTime, duration, responseCode, success, url, httpMethod, properties.ToDictionary(item => item.key, item => item.value));
        }

        public abstract void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string?>? properties = null);

        public virtual void TrackTrace(string message, (string key, string? value)[] properties)
        {
            TrackTrace(message, properties.ToDictionary(item => item.key, item => item.value));
        }

        public abstract void TrackTrace(string message, IDictionary<string, string?>? properties);

        public abstract void SetUserId(string? userId);

        public virtual void LogPreviousSessionCrashIfAny()
        {

        }

        public virtual async Task<bool> DidCrashOnPreviousExecution()
        {
            return false;
        }
    }
}
