#define Debug

using Bit.Core.Contracts;
using Prism.Logging;
using System;
using System.Collections.Generic;

namespace Bit.ViewModel.Implementations
{
    public class BitPrismLogger : ILogger
    {
        public virtual IEnumerable<ITelemetryService> TelemetryServices { get; set; } = default!;

        public virtual IExceptionHandler ExceptionHandler { get; set; } = default!;

        public virtual void Log(string message, IDictionary<string, string?> properties)
        {
            TelemetryServices.All().TrackTrace(message, properties);
        }

        public virtual void Report(Exception ex, IDictionary<string, string?>? properties)
        {
            BitExceptionHandler.Current.OnExceptionReceived(ex, properties);
        }

        public virtual void TrackEvent(string name, IDictionary<string, string?> properties)
        {
            TelemetryServices.All().TrackEvent(name, properties);
        }
    }
}
