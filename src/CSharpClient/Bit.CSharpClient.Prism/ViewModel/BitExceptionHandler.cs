#define Debug

using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bit.ViewModel
{
    public class BitExceptionHandler : IExceptionHandler
    {
        public static IExceptionHandler Current { get; set; } = new BitExceptionHandler();

        public virtual void OnExceptionReceived(Exception exp, IDictionary<string, string> properties = null)
        {
            properties = properties ?? new Dictionary<string, string>();

            if (exp != null && Debugger.IsAttached)
            {
                Debug.WriteLine($"DateTime: {DateTime.Now.ToLongTimeString()} Message: {exp}", category: "ApplicationException");
            }

            ApplicationInsightsTelemetryService.Current.TrackException(exp, properties);
            AppCenterTelemetryService.Current.TrackException(exp, properties);
            LocalTelemetryService.Current.TrackException(exp, properties);
        }
    }
}
