using Bit.Websites.Sales.Web.Services.Implementations;
using Blazored.Toast;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IStateService, StateService>();
        services.AddScoped<IExceptionHandler, ExceptionHandler>();
        services.AddBlazoredToast();

#if BlazorServer
        services.AddScoped(sp =>
        {
            HttpClient httpClient = new(sp.GetRequiredService<AppHttpClientHandler>())
            {
                BaseAddress = new Uri($"{sp.GetRequiredService<IConfiguration>()["ApiServerAddress"]}")
            };

            return httpClient;
        });
#endif

        services.AddTransient<AppHttpClientHandler>();

        return services;
    }
}
