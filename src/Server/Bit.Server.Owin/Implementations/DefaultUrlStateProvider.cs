using Bit.Core.Implementations;
using Bit.Owin.Contracts;
using Newtonsoft.Json;
using System;
using System.Net.Http;

namespace Bit.Owin.Implementations
{
    public class DefaultUrlStateProvider : IUrlStateProvider
    {
        public virtual dynamic GetState(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            string state = uri.ParseQueryString()["state"];

            if (string.IsNullOrEmpty(state))
                return new { };

            return JsonConvert.DeserializeObject<dynamic>(state, DefaultJsonContentFormatter.DeserializeSettings())!;
        }
    }
}
