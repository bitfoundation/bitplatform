using System.Reflection;
using Microsoft.Extensions.Primitives;

namespace Boilerplate.Client.Core.Components;

/// <summary>
/// Read CloudflareCacheService docs for more information.
/// </summary>
public partial class CloudflareCache : AppComponentBase
{
    [Parameter] public TimeSpan? MaxAge { get; set; }

    [Parameter] public TimeSpan? SharedMaxAge { get; set; }

    [Parameter] public string? CacheTag { get; set; }

    [AutoInject] IServiceProvider serviceProvider = default!;

    private static readonly Type? IHttpContextAccessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60");

    private static readonly PropertyInfo? HttpContextProperty = IHttpContextAccessorType?.GetProperty("HttpContext");

    private static readonly PropertyInfo? ItemsProperty = HttpContextProperty?.DeclaringType?.GetProperty("Items");

    private static readonly PropertyInfo? ResponseProperty = HttpContextProperty?.DeclaringType?.GetProperty("Response");

    private static readonly PropertyInfo? HeadersProperty = ResponseProperty?.DeclaringType?.GetProperty("Headers");

    protected async override Task OnInitAsync()
    {
        if (InPrerenderSession is false)
            return;

        var httpContextAccessor = serviceProvider.GetRequiredService(IHttpContextAccessorType!);

        var httpContext = HttpContextProperty!.GetValue(httpContextAccessor)!;

        var items = (IDictionary<object, object?>)ItemsProperty!.GetValue(httpContext)!;

        var cacheControl = $"public";

        if (MaxAge is not null)
        {
            cacheControl += $", max-age={MaxAge.Value.TotalSeconds}";
        }
        if (SharedMaxAge is not null)
        {
            cacheControl += $", s-maxage={SharedMaxAge.Value.TotalSeconds}";
        }

        items["Cache-Control-Override"] = cacheControl;
        // The Cache-Control header cannot be set here. It must be configured in the Server.Web project's Program.Middlewares.cs file.

        var response = ResponseProperty!.GetValue(httpContext);

        var headers = (IDictionary<string, StringValues>)HeadersProperty!.GetValue(response)!;

        headers["Cache-Tag"] = CacheTag ?? GetType().Name;

        await base.OnInitAsync();
    }
}
