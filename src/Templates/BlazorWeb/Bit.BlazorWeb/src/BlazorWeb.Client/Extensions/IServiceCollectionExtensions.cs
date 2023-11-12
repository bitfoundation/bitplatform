//-:cnd:noEmit
using BlazorWeb.Web.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        services.AddSharedServices();

        services.AddCascadingAuthenticationState();
        services.AddScoped<IPrerenderStateService, PrerenderStateService>();
        services.AddScoped<IExceptionHandler, ExceptionHandler>();
        services.AddScoped<IPubSubService, PubSubService>();
        services.AddBitBlazorUIServices();

        services.AddTransient<LocalizationDelegatingHandler>();
        services.AddTransient<AuthDelegatingHandler>();
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionDelegatingHandler>();
        services.AddTransient<HttpClientHandler>();

        services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped(sp => (AppAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddScoped<MessageBoxService>();

        return services;
    }
}
