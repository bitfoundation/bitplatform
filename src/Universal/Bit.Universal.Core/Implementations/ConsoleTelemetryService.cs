using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Core.Implementations
{
    public class ConsoleTelemetryService : TelemetryServiceBase
    {
        private bool _isInited = false;
        private string? _userId;

        private static ConsoleTelemetryService _current = default!;

        public static ConsoleTelemetryService Current
        {
            get => _current ?? (_current = new ConsoleTelemetryService());
            set => _current = value;
        }

        public virtual void Init()
        {
            _isInited = true;
        }

        public override bool IsConfigured()
        {
            return _isInited;
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

                Console.WriteLine($@"{eventName}
{propsAsString}");
            }
        }

        public override void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Console.WriteLine($@"{exception}
{propsAsString}");
            }
        }

        public override void TrackMetric(string name, double value, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Console.WriteLine($@"{name} : {value}
{propsAsString}");
            }
        }

        public override void TrackPageView(string name, TimeSpan duration, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Console.WriteLine($@"{name} : {duration}
{propsAsString}");
            }
        }

        public override void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string?>? properties = null)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Console.WriteLine($@"{name}
    Start time: {startTime}
    Duration: {duration}
    Response code: {responseCode}
    Success: {success}
    Http method: {httpMethod}
{propsAsString}");
            }
        }

        public override void TrackTrace(string message, IDictionary<string, string?>? properties)
        {
            if (IsConfigured())
            {
                var propsAsString = string.Join(Environment.NewLine, PopulateProperties(properties).Select(p => $"    {p.Key} : {p.Value}"));

                Console.WriteLine($@"{message}
{propsAsString}");
            }
        }
    }
}
