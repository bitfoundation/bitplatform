using System.Reflection;
using ModelContextProtocol.Server;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

internal static class DevMcpToolCatalog
{
    private static readonly Type[] ToolTypes =
    [
        typeof(DevMcpDiagnosticTools),
        typeof(DevMcpHangfireTools),
        typeof(DevMcpSchemaTools),
        typeof(DevMcpQueryTools)
    ];

    public static IReadOnlyList<McpServerTool> Tools { get; } = [.. Create()];

    private static IEnumerable<McpServerTool> Create()
    {
        foreach (var type in ToolTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null))
            {
                yield return McpServerTool.Create(method, context => ActivatorUtilities.GetServiceOrCreateInstance(context.Services, type)!);
            }
        }
    }
}
