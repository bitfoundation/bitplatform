using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Bit.Test.Implementations
{
    public class TestServerAddressesFeature : IServerAddressesFeature
    {
        private readonly string _hostUri;

        public TestServerAddressesFeature(string hostUri)
        {
            _hostUri = hostUri;
        }

        public ICollection<string> Addresses => new[] { _hostUri };

        public bool PreferHostingUrls { get; set; } = true;
    }
}
