//-:cnd:noEmit
using BlazorWeb.Client.Services.HttpMessageHandlers;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        services.AddSharedServices();

        services.TryAddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.TryAddTransient<IPrerenderStateService, PrerenderStateService>();
        services.TryAddTransient<IExceptionHandler, ExceptionHandler>();
        services.TryAddScoped<IPubSubService, PubSubService>();
        services.TryAddTransient<IStorageService, BrowserStorageService>();
        services.AddBitBlazorUIServices();

        services.TryAddTransient<RequestHeadersDelegationHandler>();
        services.TryAddTransient<AuthDelegatingHandler>();
        services.TryAddTransient<RetryDelegatingHandler>();
        services.TryAddTransient<ExceptionDelegatingHandler>();
        services.TryAddTransient<HttpClientHandler>();

        services.AddScoped<AuthenticationStateProvider, AuthenticationManager>();
        services.TryAddScoped(sp => (AuthenticationManager)sp.GetRequiredService<AuthenticationStateProvider>());

        services.TryAddTransient<MessageBoxService>();

        services.TryAddTransient<LazyAssemblyLoader>();

        return services;
    }
}
