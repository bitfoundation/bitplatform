using Microsoft.AspNet.SignalR.Client.Http;
using System;
using System.Net.Http;

namespace Bit.Signalr.Implementations
{
    public class SignalRHttpClient : DefaultHttpClient
    {
        readonly HttpMessageHandler _httpMessageHandler;

        public SignalRHttpClient(HttpMessageHandler httpMessageHandler)
        {
            _httpMessageHandler = httpMessageHandler ?? throw new ArgumentNullException(nameof(httpMessageHandler));
        }

        protected override HttpMessageHandler CreateHandler()
        {
            return _httpMessageHandler;
        }
    }
}
