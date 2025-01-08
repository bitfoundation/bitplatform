
using Bit.BlazorUI.Demo.Client.Core.Services.HttpMessageHandlers;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)

        services.TryAddTransient<IPrerenderStateService, PrerenderStateService>();

        services.TryAddSessioned<IPubSubService, PubSubService>();

        services.TryAddTransient<RequestHeadersDelegationHandler>();
        services.TryAddTransient<RetryDelegatingHandler>();
        services.TryAddTransient<ExceptionDelegatingHandler>();
        services.TryAddTransient<HttpClientHandler>();

        services.TryAddTransient<LazyAssemblyLoader>();

        services.AddBitBlazorUIServices();
        services.AddBitBlazorUIExtrasServices(trySingleton: AppRenderMode.IsBlazorHybrid);
        services.AddSharedServices();

        return services;
    }

    /// <summary>
    /// Utilizing the AddSessioned method seamlessly configures the service to function as a singleton in BlazorHybrid, and BlazorWebAssembly
    /// environments. Simultaneously, it employs per-scope registration for pre-rendering and BlazorServer scenarios
    /// </summary>
    public static IServiceCollection AddSessioned<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : class, TService
        where TService : class
    {
        if (AppRenderMode.IsBlazorHybrid || OperatingSystem.IsBrowser())
        {
            return services.AddSingleton<TService, TImplementation>();
        }
        else
        {
            return services.AddScoped<TService, TImplementation>();
        }
    }

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
    public static IServiceCollection TryAddSessioned<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : class, TService
        where TService : class
    {
        if (AppRenderMode.IsBlazorHybrid || OperatingSystem.IsBrowser())
        {
            services.TryAddSingleton<TService, TImplementation>();
        }
        else
        {
            services.TryAddScoped<TService, TImplementation>();
        }

        return services;
    }
}
