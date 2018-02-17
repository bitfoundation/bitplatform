using System;
using System.Net.Http;
using Bit.IdentityServer.Contracts;
using IdentityServer3.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultCustomLoginDataProvider : ICustomLoginDataProvider
    {
        public virtual dynamic GetCustomData(SignInMessage signInMessage)
        {
            if (signInMessage == null)
                throw new ArgumentNullException(nameof(signInMessage));

            JsonSerializerSettings jsonSerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            return JsonConvert.DeserializeObject<dynamic>(new Uri(signInMessage.ReturnUrl).ParseQueryString()["state"], jsonSerSettings);
        }
    }
}
