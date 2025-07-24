//+:cnd:noEmit
using Amazon.Runtime;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// Uses dotnet IHttpClientFactory to improve S3 implementation.
/// </summary>
public class S3HttpClientFactory(IHttpClientFactory httpClientFactory) : HttpClientFactory
{
    public override HttpClient CreateHttpClient(IClientConfig clientConfig)
    {
        return httpClientFactory.CreateClient("S3");
    }

    public override bool UseSDKHttpClientCaching(IClientConfig clientConfig)
    {
        return false; // Let dotnet IHttpClientFactory handle caching.
    }
}
