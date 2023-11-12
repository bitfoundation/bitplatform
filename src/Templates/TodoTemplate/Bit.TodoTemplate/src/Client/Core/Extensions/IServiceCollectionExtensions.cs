//-:cnd:noEmit
using TodoTemplate.Client.Core.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS and Linux)

        services.AddCascadingAuthenticationState();
        services.AddScoped<IPrerenderStateService, PrerenderStateService>();
        services.AddScoped<IPubSubService, PubSubService>();
        services.AddBitBlazorUIServices();

        services.AddTransient<LocalizationDelegatingHandler>();
        services.AddTransient<AuthDelegatingHandler>();
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionHandlerDelegatingHandler>();
        services.AddTransient<HttpClientHandler>();

        services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped(sp => (AppAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddScoped<MessageBoxService>();

        return services;
    }
}
