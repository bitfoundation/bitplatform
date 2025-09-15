using Bit.Butil;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Bit.Websites.Platform.Client.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        services.AddSharedServices();

        services.AddScoped<NavManuService>();

        services.AddTransient<IPrerenderStateService, PrerenderStateService>();
        services.AddTransient<IExceptionHandler, ExceptionHandler>();
        services.AddScoped<IPubSubService, PubSubService>();
        services.AddBitBlazorUIServices();
        services.AddBitButilServices();

        services.AddTransient<RequestHeadersDelegationHandler>();
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionDelegatingHandler>();
        services.AddTransient<HttpClientHandler>();

        services.AddTransient<MessageBoxService>();

        services.AddBitBlazorUIExtrasServices();

        services.AddScoped(sp =>
        {
            var baseAddress = sp.GetRequiredService<HttpClient>().BaseAddress!;

            var hubConnection = new HubConnectionBuilder()
                .WithStatefulReconnect()
                .WithAutomaticReconnect()
                .WithUrl(new Uri(baseAddress, "/app-hub"), options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .Build();
            return hubConnection;
        });

        return services;
    }
}
