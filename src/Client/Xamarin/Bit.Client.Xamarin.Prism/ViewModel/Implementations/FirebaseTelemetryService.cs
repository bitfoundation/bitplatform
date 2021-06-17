using Bit.Core.Contracts;
using Bit.Core.Implementations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
#if Android
    delegate void LogEventDelegate(string eventName, global::Android.OS.Bundle bundle);
    delegate void CrashlyticsSetUserIdDelegate(string? userId);
    delegate void RecordExceptionDelegate(Java.Lang.Throwable throwable);
    delegate void AnalyticsSetUserIdDelegate(string? userId);
    delegate bool DidCrashOnPreviousExecutionDelegate();
#elif iOS
    delegate void LogEventDelegate(string name, Foundation.NSDictionary<Foundation.NSString, Foundation.NSObject> nsParameters);
    delegate void SetUserIdentifierDelegate(string? identifier);
#endif

    public class FirebaseTelemetryService : TelemetryServiceBase, ITelemetryService
    {
        private bool _isInited = false;

#if Android
        private LogEventDelegate _LogEvent;
        private CrashlyticsSetUserIdDelegate _CrashlyticsSetUserId;
        private RecordExceptionDelegate _RecordException;
        private AnalyticsSetUserIdDelegate _AnalyticsSetUserId;
        private DidCrashOnPreviousExecutionDelegate _DidCrashOnPreviousExecution;
#elif iOS
        private LogEventDelegate _LogEvent;
        private SetUserIdentifierDelegate _SetUserIdentifier;
#endif

        private static FirebaseTelemetryService? _current;

        public static FirebaseTelemetryService Current
        {
            get => _current ?? (_current = new FirebaseTelemetryService());
            set => _current = value;
        }

#if Android
        public virtual void Init(global::Android.Content.Context context)
        {
            Assembly.Load("Xamarin.Firebase.Common").GetType("Firebase.FirebaseApp").GetMethods().ExtendedSingle("Finding InitializeApp method", m => m.Name == "InitializeApp" && m.GetParameters().Count() == 1).Invoke(null, new object[] { context });

            var analyticsType = Assembly.Load("Xamarin.GooglePlayServices.Measurement.Api").GetType("Firebase.Analytics.FirebaseAnalytics");
            var analyticsInstance = analyticsType.GetMethod("GetInstance").Invoke(null, new object[] { context });
            _AnalyticsSetUserId = (AnalyticsSetUserIdDelegate)Delegate.CreateDelegate(typeof(AnalyticsSetUserIdDelegate), target: analyticsInstance, method: "SetUserId", ignoreCase: false);
            _LogEvent = (LogEventDelegate)Delegate.CreateDelegate(typeof(LogEventDelegate), target: analyticsInstance, method: "LogEvent", ignoreCase: false);

            var crashlyticsType = Assembly.Load("Xamarin.Firebase.Crashlytics").GetType("Firebase.Crashlytics.FirebaseCrashlytics");
            var crashlyticsInstace = crashlyticsType.GetProperty("Instance").GetValue(null);
            _CrashlyticsSetUserId = (CrashlyticsSetUserIdDelegate)Delegate.CreateDelegate(typeof(CrashlyticsSetUserIdDelegate), target: crashlyticsInstace, method: "SetUserId", ignoreCase: false);
            _RecordException = (RecordExceptionDelegate)Delegate.CreateDelegate(typeof(RecordExceptionDelegate), target: crashlyticsInstace, method: "RecordException", ignoreCase: false);
            _DidCrashOnPreviousExecution = (DidCrashOnPreviousExecutionDelegate)Delegate.CreateDelegate(typeof(DidCrashOnPreviousExecutionDelegate), target: crashlyticsInstace, method: "DidCrashOnPreviousExecution", ignoreCase: false);

            _isInited = true;
        }
#elif iOS
        public virtual void Init()
        {
            Assembly.Load("Firebase.Core")
                .GetType("Firebase.Core.App")
                .GetMethod("Configure")
                .Invoke(null, Array.Empty<object>());

            Assembly.Load("Firebase.Crashlytics")
                .GetType("Firebase.Crashlytics.Crashlytics")
                .GetMethod("Configure")
                .Invoke(null, Array.Empty<object>());

            _LogEvent = (LogEventDelegate)Delegate.CreateDelegate(typeof(LogEventDelegate), Assembly.Load("Firebase.Analytics")
                .GetType("Firebase.Analytics.Analytics")
                .GetMethod("LogEvent"));

            object sharedInstance = Assembly.Load("Firebase.Crashlytics")
                .GetType("Firebase.Crashlytics.Crashlytics")
                .GetProperty("SharedInstance")
                .GetValue(null);

            _SetUserIdentifier = (SetUserIdentifierDelegate)Delegate.CreateDelegate(typeof(SetUserIdentifierDelegate), target: sharedInstance, "SetUserIdentifier");

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
                        bundle.PutString(param.Key.Length > 40 ? param.Key.Substring(0, 40) : param.Key, param.Value?.Length > 100 ? param.Value.Substring(0, 100) : param.Value);
                    }
                }

                _LogEvent(eventName, bundle);
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

                _LogEvent(eventName, parametersDictionary);
#endif
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                if (exception == null)
                    throw new ArgumentNullException(nameof(exception));

#if Android
                _RecordException(Java.Lang.Throwable.FromException(exception));
#else
                properties ??= new Dictionary<string, string?>();

                if (!properties.ContainsKey("Message"))
                    properties.Add("Message", exception.ToString());

                TrackEvent(exception.GetType().Name, properties);
#endif
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
                _CrashlyticsSetUserId.Invoke(userId);
                _AnalyticsSetUserId(userId);
#elif iOS
                _SetUserIdentifier(userId);
#endif
            }
        }

#if Android
        public async override Task<bool> DidCrashOnPreviousExecution()
        {
            return _DidCrashOnPreviousExecution();
        }
#endif
    }
}
