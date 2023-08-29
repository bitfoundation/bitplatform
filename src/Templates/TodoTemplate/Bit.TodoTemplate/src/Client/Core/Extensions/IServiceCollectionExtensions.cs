//-:cnd:noEmit
using TodoTemplate.Client.Core.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, and macOS)

        services.AddScoped<IPrerenderStateService, PrerenderStateService>();
        services.AddScoped<IExceptionHandler, ExceptionHandler>();
        services.AddScoped<IPubSubService, PubSubService>();
        services.AddBitBlazorUIServices();

        services.AddTransient<AppHttpClientHandler>();

        services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped(sp => (AppAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddScoped<MessageBoxService>();

        return services;
    }
}
