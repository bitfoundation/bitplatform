#define Debug

using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;
using System.Collections.Generic;

namespace Bit.ViewModel
{
    public class BitExceptionHandler : BitExceptionHandlerBase
    {
        public static new IExceptionHandler Current
        {
            get => BitExceptionHandlerBase.Current ??= new BitExceptionHandler();
            set => BitExceptionHandlerBase.Current = value;
        }

        protected override void CallTelemetryServices(Exception exp, IDictionary<string, string?>? properties)
        {
            ApplicationInsightsTelemetryService.Current.TrackException(exp, properties);
            AppCenterTelemetryService.Current.TrackException(exp, properties);
            LocalTelemetryService.Current.TrackException(exp, properties);
            FirebaseTelemetryService.Current.TrackException(exp, properties);

            base.CallTelemetryServices(exp, properties);
        }
    }
}
