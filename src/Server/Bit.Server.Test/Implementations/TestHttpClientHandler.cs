using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;

namespace Bit.Test.Implementations
{
    public class TestHttpClientHandler : DelegatingHandler
    {
        private TestEnvironmentArgs _args;

        public TestHttpClientHandler(HttpMessageHandler innerHandler, TestEnvironmentArgs args)
            : base(innerHandler)
        {
            _args = args;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.RequestUri.Segments?.Last() == "$ref")
                return new HttpResponseMessage(HttpStatusCode.OK);

            if (!request.Headers.Any(h => h.Key == "Client-Type"))
                request.Headers.Add("Client-Type", "Test");

            if (!request.Headers.Any(h => h.Key == "Client-Date-Time"))
                request.Headers.Add("Client-Date-Time", TestDependencyManager.CurrentTestDependencyManager.Resolve<IDateTimeProvider>().GetCurrentUtcDateTime().UtcDateTime.ToString("o", CultureInfo.InvariantCulture));

            if (!request.Headers.Any(h => h.Key == "X-Correlation-ID"))
                request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

            if (!request.Headers.Any(h => h.Key == "Bit-Client-Type"))
                request.Headers.Add("Bit-Client-Type", "CS-Client");

            if (!request.Headers.Any(h => h.Key == "Client-App-Version"))
                request.Headers.Add("Client-App-Version", _args.ClientArgs.AppVersion);

            if (!request.Headers.Any(h => h.Key == "Client-Culture"))
                request.Headers.Add("Client-Culture", _args.ClientArgs.Culture);

            if (!request.Headers.Any(h => h.Key == "Client-Platform"))
                request.Headers.Add("Client-Platform", "Test");

            if (!request.Headers.Any(h => h.Key == "Current-Time-Zone"))
                request.Headers.Add("Current-Time-Zone", _args.ClientArgs.CurrentTimeZone);

            if (_args.ClientArgs.DesiredTimeZone != null && !request.Headers.Any(h => h.Key == "Desired-Time-Zone"))
                request.Headers.Add("Desired-Time-Zone", _args.ClientArgs.DesiredTimeZone);

            if (!request.Headers.Any(h => h.Key == "Client-Screen-Size"))
                request.Headers.Add("Client-Screen-Size", _args.ClientArgs.ClientScreenSize);

            if (!request.Headers.Any(h => h.Key == "Client-Debug-Mode"))
                request.Headers.Add("Client-Debug-Mode", true.ToString(CultureInfo.InvariantCulture));

            HttpResponseMessage? response = null;

            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                return response;
            }
            finally
            {
                if (response != null)
                {
                    if (response.Headers.TryGetValues("Reason-Phrase", out IEnumerable<string> reasonPhrases) && reasonPhrases.Any())
                    {
                        response.ReasonPhrase = reasonPhrases.Single();
                    }
                }
            }
        }
    }
}
