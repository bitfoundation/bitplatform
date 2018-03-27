using Prism.Ioc;
using Simple.OData.Client;
using Simple.OData.Client.Core.Http;
using System;
using System.Net.Http;

namespace Bit.ViewModel.Implementations
{
    public class BitODataHttpClientFactory : Simple.OData.Client.Core.Http.IHttpClientFactory
    {
        private readonly IContainerProvider _container;

        public BitODataHttpClientFactory(IContainerProvider container)
        {
            _container = container;
        }

        public HttpClient Create(ODataClientSettings settings)
        {
            HttpClient client = _container.Resolve<HttpClient>();

            return client;
        }

        public void Clear()
        {

        }

        public void Clear(Uri uri)
        {

        }

        public HttpClient Get(Uri uri)
        {
            HttpClient client = _container.Resolve<HttpClient>();

            return client;
        }

        public virtual void Release(HttpClient httpClient)
        {

        }
    }
}
