using System.Net;

namespace Bit.Websites.Careers.Client.Shared;

public partial class ChangeResponseStatusCode : AppComponentBase
{
    [Parameter]
    public HttpStatusCode StatusCode { get; set; }

    private static readonly Type? _httpContextAccessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions");

    [AutoInject]
    public IServiceProvider _serviceProvider = default!;

    protected override Task OnInitAsync()
    {
        if (_httpContextAccessorType is not null && BlazorModeDetector.Current.IsBlazorWebAssembly())
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
