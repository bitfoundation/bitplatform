//-:cnd:noEmit

using Boilerplate.Client.Core.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS and Linux)

        services.AddTransient<IPrerenderStateService, PrerenderStateService>();
        services.AddSessioned<IPubSubService, PubSubService>();
        services.AddBitBlazorUIServices();

        services.AddTransient<RequestHeadersDelegationHandler>();
        services.AddTransient<AuthDelegatingHandler>();
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionDelegatingHandler>();
        services.AddSingleton<HttpClientHandler>();

        services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();
        services.AddScoped(sp => (AppAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddTransient<MessageBoxService>();

        return services;
    }

    public static IServiceCollection AddSessioned<TService, TImplementation>(this IServiceCollection services)
        where TImplementation: class, TService
        where TService : class
    {
#if BlazorHybrid
        return services.AddSingleton<TService, TImplementation>();
#else
        return services.AddTransient<TService, TImplementation>();
#endif
    }
}
