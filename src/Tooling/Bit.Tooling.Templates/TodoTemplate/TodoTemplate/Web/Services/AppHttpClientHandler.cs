using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;

namespace TodoTemplate.App.Services
{
    public class AppHttpClientHandler : HttpClientHandler
    {
        private readonly IJSRuntime _jsRuntime;

        public AppHttpClientHandler(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
#if ANDROID
            var access_token = Microsoft.Maui.Essentials.Preferences.Get("access_token", (string?)null);
#else
            var access_token = await _jsRuntime.InvokeAsync<string>("window.localStorage.getItem", cancellationToken, "access_token");
#endif

            if (access_token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    throw new Exception(response.StatusCode.ToString());
                }
            }

            return response;
        }
    }
}
