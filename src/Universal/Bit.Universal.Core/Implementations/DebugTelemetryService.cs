using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bit.Core.Implementations
{
    public class DebugTelemetryService : TelemetryServiceBase
    {
        private bool _isInited = false;
        private string? _userId;

        private static DebugTelemetryService _current = default!;

        public static DebugTelemetryService Current
        {
            get => _current ?? (_current = new DebugTelemetryService());
            set => _current = value;
        }

        public virtual void Init()
        {
            _isInited = true;
        }

        public override bool IsConfigured()
        {
            return _isInited && Debugger.IsAttached;
        }

        public override void SetUserId(string? userId)
        {
            _userId = userId;
        }

        public override IDictionary<string, string?> PopulateProperties(IDictionary<string, string?>? initialProps)
        {
            var props = base.PopulateProperties(initialProps);

            if (!props.ContainsKey("UserId") && _userId != null)
                props.Add("UserId", _userId);

            return props;
        }

        public override void TrackEvent(string eventName, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Debug.WriteLine($@"{eventName}
{propsAsString}", "Event");
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Debug.WriteLine($@"{exception}
{propsAsString}", "Exception");
            }
        }

        public override void TrackMetric(string name, double value, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Debug.WriteLine($@"{name} : {value}
{propsAsString}", "Metric");
            }
        }

        public override void TrackPageView(string name, TimeSpan duration, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Debug.WriteLine($@"{name} : {duration}
{propsAsString}", "PageView");
            }
        }

        public override void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Debug.WriteLine($@"{name}
    Start time: {startTime}
    Duration: {duration}
    Response code: {responseCode}
    Success: {success}
    Http method: {httpMethod}
{propsAsString}", "Request");
            }
        }

        public override void TrackTrace(string message, IDictionary<string, string?>? properties)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"{p.Key} : {p.Value}"));

                Debug.WriteLine($@"{message}
{propsAsString}", "Trace");
            }
        }
    }
}
