using TodoTemplate.App.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTodoTemplateAppServices(this IServiceCollection services)
    {
        services.AddScoped<IStateService, StateService>();
        services.AddScoped<IExceptionHandler, TodoTemplateExceptionHandler>();
//-:cnd:noEmit
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
//+:cnd:noEmit
        services.AddTransient<TodoTemplateHttpClientHandler>();

        services.AddAuthorizationCore();
        services.AddScoped<AuthenticationStateProvider, TodoTemplateAuthenticationStateProvider>();
        services.AddScoped<ITodoTemplateAuthenticationService, TodoTemplateAuthenticationService>();
        services.AddScoped(sp => (TodoTemplateAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

        return services;
    }
}
