using System.Net;

namespace BlazorWeb.Client.Shared;

/// <summary>
/// This component is used during prerendering to determine the value of the StatusCode parameter for the returned HTTP response.
/// </summary>
public partial class ChangePrerenderResponseStatusCode : AppComponentBase
{
    [Parameter]
    public HttpStatusCode StatusCode { get; set; }

    private static readonly Type? _httpContextAccessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions");

    [AutoInject]
    public IServiceProvider _serviceProvider = default!;

    protected override Task OnInitAsync()
    {
        if (_httpContextAccessorType is not null)
        {
            var httpContextAccessor = _serviceProvider.GetService(_httpContextAccessorType);

            var httpContextProperty = _httpContextAccessorType.GetProperty("HttpContext")!;

            var httpContext = httpContextProperty.GetValue(httpContextAccessor)!;

            var response = httpContext.GetType().GetProperty("Response")!.GetValue(httpContext)!;

            response.GetType().GetProperty("StatusCode")!.SetValue(response, (int)StatusCode);
        }

        return base.OnInitAsync();
    }
}
