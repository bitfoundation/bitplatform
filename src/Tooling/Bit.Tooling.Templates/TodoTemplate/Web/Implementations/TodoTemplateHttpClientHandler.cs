using System.Net.Http.Headers;

namespace TodoTemplate.App.Implementations;

public class TodoTemplateHttpClientHandler : HttpClientHandler
{
    private readonly IJSRuntime _jsRuntime;

    public TodoTemplateHttpClientHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var access_token = await _jsRuntime.InvokeAsync<string>("window.localStorage.getItem", cancellationToken, "access_token");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.ToString());
        }

        return response;
    }
}
