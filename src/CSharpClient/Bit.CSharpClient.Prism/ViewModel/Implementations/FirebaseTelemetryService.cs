using Bit.ViewModel.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bit.ViewModel.Implementations
{
    public class FirebaseTelemetryService : TelemetryServiceBase, ITelemetryService
    {
        private bool _isInited = false;

#if Android
        private global::Android.Content.Context _context = default!;
#endif

        private static FirebaseTelemetryService _current = default!;

        public static FirebaseTelemetryService Current
        {
            get => _current ?? (_current = new FirebaseTelemetryService());
            set => _current = value;
        }

#if Android
        public virtual void Init(global::Android.Content.Context context)
        {
            _context = context;
            Fabric.Fabric.With(_context, new Crashlytics.Crashlytics());
            Crashlytics.Crashlytics.HandleManagedExceptions();
            _isInited = true;
        }
#elif iOS
        public virtual void Init()
        {
            Firebase.Core.App.Configure();
            Firebase.Crashlytics.Crashlytics.Configure();
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
#if Android
                using var bundle = new global::Android.OS.Bundle();

                if (properties != null)
                {
                    foreach (var param in properties)
                    {
                        bundle.PutString(param.Key, param.Value);
                    }
                }

                Firebase.Analytics.FirebaseAnalytics.GetInstance(_context).LogEvent(eventName, bundle);
#endif

#if iOS

                var keys = new List<Foundation.NSString>();
                var values = new List<Foundation.NSString>();
                if (properties != null)
                {
                    foreach (var item in properties)
                    {
                        keys.Add(new Foundation.NSString(item.Key));
                        values.Add(new Foundation.NSString(item.Value));
                    }
                }

                var parametersDictionary = Foundation.NSDictionary<Foundation.NSString, Foundation.NSObject>.FromObjectsAndKeys(values.ToArray(), keys.ToArray(), keys.Count);

                Firebase.Analytics.Analytics.LogEvent(eventName, parametersDictionary);
#endif
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                if (exception == null)
                    throw new ArgumentNullException(nameof(exception));

                properties ??= new Dictionary<string, string?>();

                if (!properties.ContainsKey("Message"))
                    properties.Add("Message", exception.ToString());

                TrackEvent(exception.GetType().Name, properties);
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
                if (url == null)
                    throw new ArgumentNullException(nameof(url));

                properties ??= new Dictionary<string, string?>();

                if (!properties.ContainsKey("Name"))
                    properties.Add("Name", name);
                if (!properties.ContainsKey("Timestamp"))
                    properties.Add("Timestamp", startTime.ToString(CultureInfo.InvariantCulture)); /*Based on Microsoft.ApplicationInsights.DataContracts.RequestTelemetry*/
                if (!properties.ContainsKey("Duration"))
                    properties.Add("Duration", duration.ToString());
                if (!properties.ContainsKey("Success"))
                    properties.Add("Success", success.ToString(CultureInfo.InvariantCulture));
                if (!properties.ContainsKey("Url"))
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
#if Android
                Crashlytics.Crashlytics.SetUserIdentifier(userId);
#elif iOS
                Firebase.Crashlytics.Crashlytics.SharedInstance.SetUserIdentifier(userId);
#endif
            }
        }
    }
}