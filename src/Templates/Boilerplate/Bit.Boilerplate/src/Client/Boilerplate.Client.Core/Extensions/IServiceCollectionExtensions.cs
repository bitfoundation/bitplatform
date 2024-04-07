//+:cnd:noEmit
//#if (offlineDb == true)
using System.Diagnostics.CodeAnalysis;
using Boilerplate.Client.Core.Data;
//#endif
using Boilerplate.Client.Core.Services.HttpMessageHandlers;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in client side (Web, Android, iOS, Windows, macOS) and server side (during pre rendering)

        services.TryAddTransient<IPrerenderStateService, PrerenderStateService>();

        services.TryAddSessioned<IPubSubService, PubSubService>();
        services.TryAddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.TryAddTransient<IStorageService, BrowserStorageService>();

        services.TryAddKeyedTransient<DelegatingHandler, RequestHeadersDelegationHandler>("DefaultMessageHandler");
        services.TryAddTransient<AuthDelegatingHandler>();
        services.TryAddTransient<RetryDelegatingHandler>();
        services.TryAddTransient<ExceptionDelegatingHandler>();
        services.TryAddSessioned<HttpClientHandler>();

        services.AddSessioned<AuthenticationStateProvider, AuthenticationManager>(); // Use 'Add' instead of 'TryAdd' to override the aspnetcore's default AuthenticationStateProvider.
        services.TryAddSessioned(sp => (AuthenticationManager)sp.GetRequiredService<AuthenticationStateProvider>());

        services.TryAddTransient<MessageBoxService>();
        services.TryAddTransient<LazyAssemblyLoader>();

        services.TryAddTransient(sp => AppJsonContext.Default.Options);
        services.AddTypedHttpClients();

        services.AddBitButilServices();
        services.AddBitBlazorUIServices();

        //#if (offlineDb == true)
        services.AddBesqlDbContextFactory<OfflineDbContext>();
        //#endif

        services.AddSharedProjectServices();
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

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
    public static IServiceCollection TryAddSessioned<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        if (AppRenderMode.IsBlazorHybrid || OperatingSystem.IsBrowser())
        {
            services.TryAdd(ServiceDescriptor.Singleton(implementationFactory));
        }
        else
        {
            services.TryAdd(ServiceDescriptor.Scoped(implementationFactory));
        }

        return services;
    }

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
    public static void TryAddSessioned<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services)
        where TService : class
    {
        if (AppRenderMode.IsBlazorHybrid || OperatingSystem.IsBrowser())
        {
            services.TryAddSingleton(typeof(TService), typeof(TService));
        }
        else
        {
            services.TryAddScoped(typeof(TService), typeof(TService));
        }
    }
}
