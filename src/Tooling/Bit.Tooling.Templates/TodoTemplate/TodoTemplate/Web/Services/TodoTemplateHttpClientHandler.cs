using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;

namespace TodoTemplate.App.Services
{
    public class TodoTemplateHttpClientHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ToString());
            }

            return response;
        }
    }
}
