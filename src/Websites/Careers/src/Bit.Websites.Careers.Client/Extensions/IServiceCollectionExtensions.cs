using Bit.Websites.Careers.Client.Services.HttpMessageHandlers;

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
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionDelegatingHandler>();
        services.AddTransient<HttpClientHandler>();

        services.AddTransient<MessageBoxService>();

        return services;
    }
}
