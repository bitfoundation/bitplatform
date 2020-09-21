using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblyConnectivity : IConnectivity
    {
        public NetworkAccess NetworkAccess => NetworkAccess.Unknown;

        public IEnumerable<ConnectionProfile> ConnectionProfiles => Enumerable.Empty<ConnectionProfile>();

        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged = default!;
    }
}
