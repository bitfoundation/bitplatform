using Polly;
using System.Net.Http;

namespace Bit.Http.Contracts
{
    public delegate IAsyncPolicy<HttpResponseMessage> IPollyHttpResponseMessagePolicyFactory(HttpRequestMessage requestMessage);
}
