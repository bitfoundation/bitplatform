public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTodoTemplateSharedServices(this IServiceCollection services)
    {
        services.AddScoped<TodoTemplateHttpClientHandler>();

        return services;
    }
}
