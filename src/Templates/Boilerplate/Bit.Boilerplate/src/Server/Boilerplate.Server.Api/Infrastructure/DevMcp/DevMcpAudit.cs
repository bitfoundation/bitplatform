using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

internal static class DevMcpAudit
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Filter(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next)
    {
        return (context, cancellationToken) => Invoke(next, context, cancellationToken);
    }

    private static async ValueTask<CallToolResult> Invoke(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next,
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken)
    {
        var services = context.Services;
        var httpContext = services.GetService<IHttpContextAccessor>()?.HttpContext;
        if (httpContext?.Request.Path.StartsWithSegments("/dev-mcp") is not true)
            return await next(context, cancellationToken);

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Boilerplate.Server.Api.Infrastructure.DevMcp");
        var audit = services.GetService<DevMcpAuditContext>();
        var user = httpContext.User;
        var tool = context.Params?.Name;
        var arguments = context.Params?.Arguments is { Count: > 0 } args
            ? DevMcpJson.Serialize(args)
            : "{}";

        var started = Stopwatch.GetTimestamp();
        var succeeded = false;
        try
        {
            var result = await next(context, cancellationToken);
            succeeded = result.IsError is not true;
            return result;
        }
        finally
        {
            var durationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            logger.LogInformation(
                "Dev MCP {Tool} invoked by {UserId} session {SessionId}. Succeeded: {Succeeded}. DurationMs: {DurationMs}. RowCount: {RowCount}. Arguments: {Arguments}",
                tool,
                user.IsAuthenticated() ? user.GetUserId() : Guid.Empty,
                user.IsAuthenticated() ? user.GetSessionId() : Guid.Empty,
                succeeded,
                durationMs,
                audit?.RowCount,
                arguments);
        }
    }
}
