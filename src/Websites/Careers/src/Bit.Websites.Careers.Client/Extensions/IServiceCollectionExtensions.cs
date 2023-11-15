using Bit.Websites.Careers.Client.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        services.AddSharedServices();

        services.AddScoped<IPrerenderStateService, PrerenderStateService>();
        services.AddScoped<IExceptionHandler, ExceptionHandler>();
        services.AddScoped<IPubSubService, PubSubService>();
        services.AddBitBlazorUIServices();

        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionDelegatingHandler>();
        services.AddTransient<HttpClientHandler>();

        services.AddScoped<MessageBoxService>();

        return services;
    }
}
