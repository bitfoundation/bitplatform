#define Debug

using Bit.Core.Contracts;
using Bit.Core.Exceptions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Core.Implementations
{
    public class BitExceptionHandlerBase : IExceptionHandler
    {
        public static IExceptionHandler Current { get; set; } = default!;

        public virtual void OnExceptionReceived(Exception exp, (string key, string? value)[] properties)
        {
            OnExceptionReceived(exp, properties.ToDictionary(item => item.key, item => item.value));
        }

        public virtual void OnExceptionReceived(Exception exp, IDictionary<string, string?>? properties = null)
        {
            if (exp == null)
                throw new ArgumentNullException(nameof(exp));

            properties = properties ?? new Dictionary<string, string?>();

            if (exp is IExceptionData exceptionData && exceptionData.Items != null)
            {
                foreach (var item in exceptionData.Items)
                {
                    properties.Add(item);
                }
            }

            CallTelemetryServices(exp, properties);
        }

        protected virtual void CallTelemetryServices(Exception exp, IDictionary<string, string?>? properties)
        {
            DebugTelemetryService.Current.TrackException(exp, properties);
            ConsoleTelemetryService.Current.TrackException(exp, properties);
        }
    }
}
