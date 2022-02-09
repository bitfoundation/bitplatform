using System.Net.Http.Headers;

namespace TodoTemplate.App.Services.Implementations;

public class TodoTemplateHttpClientHandler : HttpClientHandler
{
    private readonly IJSRuntime _jsRuntime;

    public TodoTemplateHttpClientHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var access_token = await _jsRuntime.InvokeAsync<string>("todoTemplate.getCookie", cancellationToken, "access_token");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

        var response = await base.SendAsync(request, cancellationToken);

        return response;
    }
}
