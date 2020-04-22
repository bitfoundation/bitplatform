#define Debug

using Bit.ViewModel.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bit.ViewModel
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

            if (Debugger.IsAttached)
            {
                Console.WriteLine($"DateTime: {DateTime.Now.ToLongTimeString()} Message: {exp}");
                Debug.WriteLine($"DateTime: {DateTime.Now.ToLongTimeString()} Message: {exp}", category: "ApplicationException");
            }

            CallTelemetryServices(exp, properties);
        }

        protected virtual void CallTelemetryServices(Exception exp, IDictionary<string, string?>? properties)
        {

        }
    }
}
