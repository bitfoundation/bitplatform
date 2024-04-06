﻿using BlazorApplicationInsights;
using BlazorApplicationInsights.Interfaces;
using Boilerplate.Client.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Boilerplate.Client.Web;

public static partial class Program
{
    public static void ConfigureServices(this WebAssemblyHostBuilder builder)
    {
        // Services being registered here can get injected in web project only.

        var services = builder.Services;
        var configuration = builder.Configuration;

        configuration.AddClientConfigurations();

        Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

        if (apiServerAddress!.IsAbsoluteUri is false)
        {
            apiServerAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiServerAddress);
        }

        services.TryAddTransient(sp => new HttpClient(sp.GetRequiredKeyedService<DelegatingHandler>("DefaultMessageHandler")) { BaseAddress = apiServerAddress });

        services.AddSingleton<IApplicationInsights, WebApplicationInsights>();
        services.AddBlazorApplicationInsights(x =>
        {
            x.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        }, async appInsights =>
        {
            var serviceProvider = ((WebApplicationInsights)appInsights).ServiceProvider;
            var authManager = serviceProvider.GetRequiredService<AuthenticationManager>();
            var user = (await authManager.GetAuthenticationStateAsync()).User;

            async Task SetLoggerAuthenticationState(AuthenticationState state)
            {
                try
                {
                    var user = state.User;
                    if (user.IsAuthenticated())
                    {
                        await appInsights.SetAuthenticatedUserContext(user.GetUserId().ToString());
                    }
                    else
                    {
                        await appInsights.ClearAuthenticatedUserContext();
                    }
                }
                catch (Exception exp)
                {
                    serviceProvider.GetRequiredService<IExceptionHandler>().Handle(exp);
                }
            }

            await SetLoggerAuthenticationState(await authManager.GetAuthenticationStateAsync());

            authManager.AuthenticationStateChanged += async state => await SetLoggerAuthenticationState(await state);
        });

        services.AddClientWebProjectServices();
    }

    public static void AddClientWebProjectServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in both web project and server (during prerendering).

        services.TryAddTransient<IBitDeviceCoordinator, WebDeviceCoordinator>();
        services.TryAddTransient<IExceptionHandler, WebExceptionHandler>();

        services.AddClientCoreProjectServices();
    }
}
