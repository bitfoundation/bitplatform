//+:cnd:noEmit
using ModelContextProtocol.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class DevMcpServiceCollectionExtensions
{
    public static IMcpServerBuilder AddDevMcp(this IServiceCollection services)
    {
        services.AddScoped<DevMcpDiagnosticTools>();
        services.AddScoped<DevMcpHangfireTools>();
        services.AddScoped<DevMcpSchemaTools>();
        services.AddScoped<DevMcpQueryTools>();

        return services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.ConfigureSessionOptions = (httpContext, mcpOptions, _) =>
                {
                    if (httpContext.Request.Path.StartsWithSegments("/dev-mcp"))
                    {
                        mcpOptions.ToolCollection = [];
                        foreach (var tool in DevMcpToolCatalog.Tools)
                            mcpOptions.ToolCollection.Add(tool);
                    }
                    return Task.CompletedTask;
                };
            })
            .AddAuthorizationFilters();
    }
}
