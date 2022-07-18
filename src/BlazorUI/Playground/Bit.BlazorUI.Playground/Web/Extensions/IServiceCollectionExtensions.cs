using System;
using System.Net.Http;
using Bit.BlazorUI.Playground.Web.Services;
using Bit.BlazorUI.Playground.Web.Services.Contracts;
using Bit.BlazorUI.Playground.Web.Services.Implementations;
using Microsoft.Extensions.Configuration;
#if BlazorWebAssembly
using Microsoft.AspNetCore.Components.WebAssembly.Services;
#endif

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPlaygroundServices(this IServiceCollection services)
    {
        services.AddScoped<IExceptionHandler, ExceptionHandler>();

#if BlazorServer || BlazorHybrid
        services.AddScoped(sp =>
        {
            HttpClient httpClient = new()
            {
                BaseAddress = new Uri($"{sp.GetRequiredService<IConfiguration>()["ApiServerAddress"]}api/")
            };

            return httpClient;
        });
#endif

        services.AddScoped<NavManuService>();

#if BlazorWebAssembly
        services.AddScoped<LazyAssemblyLoader>();
#endif

        return services;
    }
}
