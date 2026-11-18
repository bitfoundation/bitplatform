using Microsoft.Extensions.Http;

namespace Bit.BevTools;

/// <summary>Adds <see cref="BevToolsHttpMessageHandler"/> to every <c>IHttpClientFactory</c> pipeline.</summary>
internal sealed class BevToolsHttpFilter(BevToolsOptions options, BevToolsRegistry registry) : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) => builder =>
    {
        next(builder);
        if (registry.IsEnabled && options.TrackHttp)
        {
            // Outermost: the caller's synchronization context is still current here, so this is where the issuing
            // session can be inferred. A handler further in that awaits with ConfigureAwait(false) - a retry delay, a
            // token fetch - leaves everything after it on the thread pool, where it no longer can.
            builder.AdditionalHandlers.Insert(0, new BevToolsHttpSessionHandler());

            // Innermost, i.e. closest to the primary handler: every attempt a resilience/retry handler makes is a real
            // network request and is recorded as one. Sitting outermost would hide retries behind a single record,
            // which is exactly the pattern "why is this request repeating?" needs to show.
            builder.AdditionalHandlers.Add(new BevToolsHttpMessageHandler(options));
        }
    };
}
