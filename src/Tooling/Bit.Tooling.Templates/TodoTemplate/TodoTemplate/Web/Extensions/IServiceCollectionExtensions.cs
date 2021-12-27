namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTodoTemplateServices(this IServiceCollection services)
    {
        services.AddScoped<IToastService, ToastService>();


        return services;
    }
}
