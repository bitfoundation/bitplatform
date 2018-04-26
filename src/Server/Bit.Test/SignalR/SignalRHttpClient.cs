using System;
using System.Net.Http;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Bit.Test.SignalR
{
    public class SignalRHttpClient : DefaultHttpClient
    {
        private readonly HttpMessageHandler _httpMessageHandler;

        public SignalRHttpClient(HttpMessageHandler httpMessageHandler)
        {
            if (httpMessageHandler == null)
                throw new ArgumentNullException(nameof(httpMessageHandler));

            _httpMessageHandler = httpMessageHandler;
        }

        protected override HttpMessageHandler CreateHandler()
        {
            return _httpMessageHandler;
        }
    }
}
