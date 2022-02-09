using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTodoTemplateServices(this IServiceCollection services)
    {
        services.AddScoped<IToastService, ToastService>();
        services.AddScoped<StateService>();

#if BlazorServer || BlazorHybrid
        services.AddScoped(sp =>
        {
            HttpClient httpClient = new(sp.GetRequiredService<TodoTemplateHttpClientHandler>())
            {
                BaseAddress = new Uri($"{sp.GetRequiredService<IConfiguration>()["ApiServerAddress"]}api/")
            };

            return httpClient;
        });
#endif

        services.AddScoped<TodoTemplateHttpClientHandler>();

        services.AddAuthorizationCore();
        services.AddScoped<AuthenticationStateProvider, TodoTemplateAuthenticationStateProvider>();
        services.AddScoped<ITodoTemplateAuthenticationService, TodoTemplateAuthenticationService>();
        services.AddScoped(sp => (TodoTemplateAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

        return services;
    }
}
