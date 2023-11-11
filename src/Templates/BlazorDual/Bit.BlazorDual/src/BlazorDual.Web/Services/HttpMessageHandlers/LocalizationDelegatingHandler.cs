using System.Net.Http.Headers;

namespace BlazorDual.Web.Services.HttpMessageHandlers;

public class LocalizationDelegatingHandler
    : DelegatingHandler
{
    public LocalizationDelegatingHandler(AuthorizationDelegatingHandler handler) 
        : base(handler)
    {

    }

    public LocalizationDelegatingHandler()
    {

    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if MultilingualEnabled
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentCulture.Name));
#endif

        return await base.SendAsync(request, cancellationToken);
    }
}

