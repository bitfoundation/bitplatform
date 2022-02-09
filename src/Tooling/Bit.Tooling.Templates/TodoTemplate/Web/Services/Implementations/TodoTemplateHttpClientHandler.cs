using System.Net.Http.Headers;

namespace TodoTemplate.App.Services.Implementations;

public class TodoTemplateHttpClientHandler : HttpClientHandler
{
#if BlazorServer || BlazorHybrid
    private readonly IJSRuntime _jsRuntime;

    public TodoTemplateHttpClientHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
#endif

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if BlazorServer || BlazorHybrid
        var access_token = await _jsRuntime.InvokeAsync<string>("todoTemplate.getCookie", cancellationToken, "access_token");
        if(access_token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
        }
#endif

        var response = await base.SendAsync(request, cancellationToken);

        return response;
    }
}
