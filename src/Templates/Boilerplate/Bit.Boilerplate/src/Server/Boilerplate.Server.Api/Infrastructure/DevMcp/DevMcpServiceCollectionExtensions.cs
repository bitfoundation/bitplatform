//+:cnd:noEmit
using ModelContextProtocol.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class DevMcpServiceCollectionExtensions
{
    public static IMcpServerBuilder AddDevMcp(this IServiceCollection services)
    {
        services.AddScoped<DevMcpConfigurationTools>();
        services.AddScoped<DevMcpHangfireTools>();
        services.AddScoped<DevMcpSchemaTools>();
        services.AddScoped<DevMcpQueryTools>();
        services.AddScoped<DevMcpAuditContext>();

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
            .AddAuthorizationFilters()
            .WithRequestFilters(filters => filters.AddCallToolFilter(DevMcpAudit.Filter));
    }
}
