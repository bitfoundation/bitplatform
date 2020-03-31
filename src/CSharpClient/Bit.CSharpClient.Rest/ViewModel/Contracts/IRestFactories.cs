using Polly;
using System.Net.Http;

namespace Bit.ViewModel.Contracts
{
    public delegate IAsyncPolicy<HttpResponseMessage> IPollyHttpResponseMessagePolicyFactory(HttpRequestMessage requestMessage);
}
