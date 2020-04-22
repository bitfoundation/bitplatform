using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Bit.ViewModel.Implementations
{
    public static class DefaultRestFactories
    {
        public static IAsyncPolicy<HttpResponseMessage> BuildHttpPollyPolicy(HttpRequestMessage requestMessage)
        {
            // https://github.com/App-vNext/Polly.Extensions.Http/blob/master/src/Polly.Extensions.Http/HttpPolicyExtensions.cs

            IAsyncPolicy<HttpResponseMessage> policy = Policy.Handle<HttpRequestException>() // HandleTransientHttpError
                .OrResult<HttpResponseMessage>((response) =>
                {
                    if (response.ReasonPhrase == "KnownError" || (response.Headers.TryGetValues("Reason-Phrase", out IEnumerable<string> reasonPhrases) && reasonPhrases.Any(rp => rp == "KnownError"))) // Bit Policy
                        return false;
                    return (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout; // TransientHttpStatusCodePredicate
                })
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                });

            return policy;
        }

    }
}
