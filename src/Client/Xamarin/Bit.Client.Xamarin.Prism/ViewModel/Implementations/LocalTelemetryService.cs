using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.ViewModel.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.ViewModel.Implementations
{
    public class ThingProperty
    {
        public string Key { get; set; } = default!;

        public string? Value { get; set; }
    }

    public class TrackedThing
    {
        public ThingProperty[] Properties { get; set; } = Array.Empty<ThingProperty>();

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Properties.Select(p => $"{p.Key} : {p.Value}"));
        }
    }

    public class TrackedEvent : TrackedThing
    {
        public string Name { get; set; } = default!;

        public override string ToString()
        {
            return $@"{Name}
{base.ToString()}";
        }
    }

    public class TrackedException : TrackedThing
    {
        public Exception Exception { get; set; } = default!;

        public override string ToString()
        {
            return $@"{Exception}
{base.ToString()}";
        }
    }

    public class TrackedMetric : TrackedThing
    {
        public string Name { get; set; } = default!;

        public double Value { get; set; }

        public override string ToString()
        {
            return $@"{Name} : {Value}
{base.ToString()}";
        }
    }

    public class TrackedPageView : TrackedThing
    {
        public string Name { get; set; } = default!;

        public TimeSpan Duration { get; set; }

        public override string ToString()
        {
            return $@"{Name} : {Duration}
{base.ToString()}";
        }
    }

    public class TrackedRequest : TrackedThing
    {
        public string Name { get; set; } = default!;

        public DateTimeOffset StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string ResponseCode { get; set; } = default!;

        public bool Success { get; set; }

        public string HttpMethod { get; set; } = default!;

        public override string ToString()
        {
            return $@"{Name}
Start time: {StartTime}
Duration: {Duration}
Response code: {ResponseCode}
Success: {Success}
Http method: {HttpMethod}
{base.ToString()}";
        }
    }

    public class TrackedTrace : TrackedThing
    {
        public string Message { get; set; } = default!;

        public override string ToString()
        {
            return $@"{Message}
{base.ToString()}";
        }
    }

    public class LocalTelemetryService : TelemetryServiceBase, ITelemetryService
    {
        private bool _isInited = false;

        private static LocalTelemetryService _current = default!;

        public static LocalTelemetryService Current
        {
            get => _current ?? (_current = new LocalTelemetryService());
            set => _current = value;
        }

        private string? _UserId;

#if UWP || Android || iOS
        public virtual void Init()
        {
            _isInited = true;
        }
#endif

        public override bool IsConfigured()
        {
            return _isInited;
        }

        public override void SetUserId(string? userId)
        {
            _UserId = userId;
        }

        private readonly ConcurrentDictionary<DateTimeOffset, TrackedThing> _TrackedThings = new ConcurrentDictionary<DateTimeOffset, TrackedThing>();

        public IEnumerable<TrackedThing> TrackedThings
        {
            get => _TrackedThings.OrderByDescending(item => item.Key).Select(item => item.Value).ToArray();
            set { }
        }

        public virtual void ClearThings()
        {
            _TrackedThings.Clear();
        }

        protected virtual void AddNewThing(TrackedThing trackedThing)
        {
            if (IsConfigured())
            {
                _TrackedThings.TryAdd(DateTimeOffset.UtcNow, trackedThing);
            }
        }

        public override IDictionary<string, string?> PopulateProperties(IDictionary<string, string?>? initialProps)
        {
            var props = base.PopulateProperties(initialProps);

            if (!props.ContainsKey("UserId") && _UserId != null)
                props.Add("UserId", _UserId);

            return props;
        }

        public override void TrackEvent(string eventName, IDictionary<string, string?>? properties = null)
        {
            properties = PopulateProperties(properties);

            AddNewThing(new TrackedEvent
            {
                Name = eventName,
                Properties = properties.Select(item => new ThingProperty
                {
                    Key = item.Key,
                    Value = item.Value
                }).ToArray()
            });
        }

        public override void TrackException(Exception exception, IDictionary<string, string?>? properties = null)
        {
            properties = PopulateProperties(properties);

            AddNewThing(new TrackedException
            {
                Exception = exception,
                Properties = properties.Select(item => new ThingProperty
                {
                    Key = item.Key,
                    Value = item.Value
                }).ToArray()
            });
        }

        public override void TrackMetric(string name, double value, IDictionary<string, string?>? properties = null)
        {
            properties = PopulateProperties(properties);

            AddNewThing(new TrackedMetric
            {
                Name = name,
                Value = value,
                Properties = properties.Select(item => new ThingProperty
                {
                    Key = item.Key,
                    Value = item.Value
                }).ToArray()
            });
        }

        public override void TrackPageView(string name, TimeSpan duration, IDictionary<string, string?>? properties = null)
        {
            properties = PopulateProperties(properties);

            AddNewThing(new TrackedPageView
            {
                Name = name,
                Duration = duration,
                Properties = properties.Select(item => new ThingProperty
                {
                    Key = item.Key,
                    Value = item.Value
                }).ToArray()
            });
        }

        public override void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success, Uri url, string httpMethod, IDictionary<string, string?>? properties = null)
        {
            properties = PopulateProperties(properties);

            AddNewThing(new TrackedRequest
            {
                Duration = duration,
                Name = name,
                Properties = properties.Select(item => new ThingProperty
                {
                    Key = item.Key,
                    Value = item.Value
                }).ToArray(),
                HttpMethod = httpMethod,
                ResponseCode = responseCode,
                StartTime = startTime,
                Success = success
            });
        }

        public override void TrackTrace(string message, IDictionary<string, string?>? properties)
        {
            properties = PopulateProperties(properties);

            AddNewThing(new TrackedTrace
            {
                Message = message,
                Properties = properties.Select(item => new ThingProperty
                {
                    Key = item.Key,
                    Value = item.Value
                }).ToArray()
            });
        }

        public virtual async Task OpenConsole()
        {
            try
            {
                var navService = ((BitApplication)Application.Current).NavigationService;
                navService = navService.CurrentPageNavService ?? navService;
                await navService.NavigateAsync("BitConsole").ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }
    }
}
