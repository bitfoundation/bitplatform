using Bit.ViewModel.Implementations;
using System.Collections.Generic;

namespace Bit.ViewModel.Contracts
{
    public static class ITelemetryServiceExtensions
    {
        public static ITelemetryService All(this IEnumerable<ITelemetryService> telemetryServices)
        {
            return new TelemetryServiceAggregator(telemetryServices);
        }
    }
}
