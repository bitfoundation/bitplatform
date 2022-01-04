namespace TodoTemplate.App.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAppServices(this IServiceCollection services)
        {
            services.AddScoped(sp =>
            {
                HttpClient httpClient = new(sp.GetRequiredService<TodoTemplateHttpClientHandler>())
                {
                    BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>()["ApiServerAddress"])
                };

                return httpClient;
            });

            services.AddScoped<TodoTemplateHttpClientHandler>();

            return services;
        }
    }
}
