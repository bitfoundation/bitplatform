using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblyWebAuthenticator : IWebAuthenticator
    {
        public virtual Task<WebAuthenticatorResult> AuthenticateAsync(Uri url, Uri callbackUrl)
        {
            throw new NotImplementedException();
        }
    }
}
