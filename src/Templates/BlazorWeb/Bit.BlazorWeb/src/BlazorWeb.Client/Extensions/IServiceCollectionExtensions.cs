//-:cnd:noEmit
using BlazorWeb.Client.Services.HttpMessageHandlers;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        services.AddSharedServices();

        services.AddTransient<IPrerenderStateService, PrerenderStateService>();
        services.AddTransient<IExceptionHandler, ExceptionHandler>();
        services.AddScoped<IPubSubService, PubSubService>();
        services.AddBitBlazorUIServices();

        services.AddTransient<RequestHeadersDelegationHandler>();
        services.AddTransient<AuthDelegatingHandler>();
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionDelegatingHandler>();
        services.AddScoped<HttpClientHandler>();

        services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();
        services.AddScoped(sp => (AppAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddTransient<MessageBoxService>();

        services.AddTransient<LazyAssemblyLoader>();

        return services;
    }
}
