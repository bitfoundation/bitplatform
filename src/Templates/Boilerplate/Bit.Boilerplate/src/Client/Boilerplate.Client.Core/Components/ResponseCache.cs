using System.Reflection;

namespace Boilerplate.Client.Core.Components;

/// <summary>
/// Read CloudflareCacheService docs for more information.
/// </summary>
public partial class ResponseCache : AppComponentBase
{
    [Parameter] public TimeSpan MaxAge { get; set; } = TimeSpan.Zero;

    [Parameter] public TimeSpan SharedMaxAge { get; set; } = TimeSpan.Zero;

    [AutoInject] IServiceProvider serviceProvider = default!;

    private static readonly Type? IHttpContextAccessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60");

    private static readonly PropertyInfo? HttpContextProperty = IHttpContextAccessorType?.GetProperty("HttpContext");

    private static readonly Type? HttpContextType = Type.GetType("Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60");

    private static readonly PropertyInfo? ItemsProperty = HttpContextType?.GetProperty("Items");

    protected async override Task OnInitAsync()
    {
        if (InPrerenderSession is false)
            return;

        var httpContextAccessor = serviceProvider.GetRequiredService(IHttpContextAccessorType!);

        var httpContext = HttpContextProperty!.GetValue(httpContextAccessor)!;

        var items = (IDictionary<object, object?>)ItemsProperty!.GetValue(httpContext)!;

        items["Cache-Control-Override"] = $"public, s-maxage={SharedMaxAge.TotalSeconds}, max-age={MaxAge.TotalSeconds}";
        // See Server.Web's Program.Middlewares.cs

        await base.OnInitAsync();
    }
}
