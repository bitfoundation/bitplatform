namespace TodoTemplate.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTodoTemplateSharedServices(this IServiceCollection services)
        {
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            return services;
        }
    }

}
