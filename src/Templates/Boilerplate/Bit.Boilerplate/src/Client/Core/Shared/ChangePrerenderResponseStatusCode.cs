using System.Net;

namespace Boilerplate.Client.Core.Shared;

/// <summary>
/// This component is used during prerendering to determine the value of the StatusCode parameter for the returned HTTP response.
/// </summary>
public partial class ChangePrerenderResponseStatusCode : AppComponentBase
{
    [Parameter] public HttpStatusCode StatusCode { get; set; }

    private static readonly Type? httpContextAccessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions");

    [AutoInject] public IServiceProvider serviceProvider = default!;

    protected override Task OnInitAsync()
    {
        if (httpContextAccessorType is not null && BlazorModeDetector.Current.IsBlazorWebAssembly())
        {
            var httpContextAccessor = serviceProvider.GetService(httpContextAccessorType);

            var httpContextProperty = httpContextAccessorType.GetProperty("HttpContext")!;

            var httpContext = httpContextProperty.GetValue(httpContextAccessor)!;

            var response = httpContext.GetType().GetProperty("Response")!.GetValue(httpContext)!;

            response.GetType().GetProperty("StatusCode")!.SetValue(response, (int)StatusCode);
        }

        return base.OnInitAsync();
    }
}
