using System;
using System.Net.Http;
using Bit.BlazorUI.Demo.Web.Services;
using Bit.BlazorUI.Demo.Web.Services.Contracts;
using Bit.BlazorUI.Demo.Web.Services.Implementations;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        services.AddScoped<IExceptionHandler, ExceptionHandler>();

#if BlazorServer || BlazorHybrid
        services.AddScoped(sp =>
        {
            HttpClient httpClient = new()
            {
                BaseAddress = new Uri($"{sp.GetRequiredService<IConfiguration>()["ApiServerAddress"]}")
            };

            return httpClient;
        });
#endif

        services.AddScoped<NavManuService>();

        return services;
    }
}
