namespace TodoTemplate.App.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AppServices(this IServiceCollection services)
        {
            services.AddScoped(sp =>
            {
                HttpClient httpClient = new(sp.GetRequiredService<AppHttpClientHandler>()) { BaseAddress = new Uri("https://localhost:5001/api/") };

                return httpClient;
            });

            services.AddScoped<AppHttpClientHandler>();

            return services;
        }
    }
}
