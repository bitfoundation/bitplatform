using Microsoft.AspNetCore.Http.Metadata;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Bswup.Demo.Server.Controllers;

/// <summary>
/// What both MCP route groups carry (see Program.cs): the request-body cap, which routing applies
/// to the request the way it applies a [RequestSizeLimit], and the shape
/// <see cref="McpErrorMiddleware"/> answers a refused request in. One object, so the limit cannot
/// be put on an endpoint without the answer that explains it.
/// </summary>
public sealed class McpEndpointMetadata(long maxRequestBodySize, bool jsonRpc) : IRequestSizeLimitMetadata
{
    public long? MaxRequestBodySize => maxRequestBodySize;

    /// <summary>
    /// True on the MCP transport, whose clients read a JSON-RPC error; false on the HTTP mirror,
    /// which answers problem details like the rest of its errors.
    /// </summary>
    public bool JsonRpc => jsonRpc;
}

/// <summary>
/// Answers a refused request to the MCP endpoints or their HTTP mirror with the reason, in the
/// shape that endpoint's clients read, instead of letting it reach the exception handler.
/// <para>
/// Two kinds of refusal get here. A tool rejecting its input (<see cref="ToolInputException"/>),
/// which only happens on the mirror: over MCP the call-tool filter has already answered it. And a
/// request rejected while its body is read (<see cref="BadHttpRequestException"/>), above all a
/// body past <see cref="McpEndpointMetadata.MaxRequestBodySize"/>, which on /mcp is thrown inside
/// the SDK's transport, where no MVC filter runs. Left to the exception handler, both were logged
/// as server failures, and in production /error was re-executed as a POST that the antiforgery
/// middleware rejected with a misleading 400 (the trap the rate limiter's OnRejected in Program.cs
/// describes). The client learned nothing about what to fix.
/// </para>
/// <para>
/// Endpoints without <see cref="McpEndpointMetadata"/> (the site's own pages) are left to the
/// exception handler as before, and so is every other exception.
/// </para>
/// </summary>
public class McpErrorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (context.Response.HasStarted is false &&
                                          context.GetEndpoint()?.Metadata.GetMetadata<McpEndpointMetadata>() is { } endpoint &&
                                          Refuse(exception, endpoint) is { } refusal)
        {
            context.Response.Clear();

            var result = endpoint.JsonRpc
                ? Results.Json(new JsonRpcError { Error = new() { Code = (int)McpErrorCode.InvalidRequest, Message = refusal.Reason } },
                               McpJsonUtilities.DefaultOptions, statusCode: refusal.StatusCode)
                : Results.Problem(detail: refusal.Reason, statusCode: refusal.StatusCode);

            await result.ExecuteAsync(context);
        }
    }

    private static (int StatusCode, string Reason)? Refuse(Exception exception, McpEndpointMetadata endpoint) => exception switch
    {
        ToolInputException => (StatusCodes.Status400BadRequest, exception.Message),

        // Said in the tool's terms rather than the server's: the agent that sent this needs to know
        // how to split it, which "request body too large" does not tell it.
        BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } =>
            (StatusCodes.Status413PayloadTooLarge,
             $"The request body is larger than the {endpoint.MaxRequestBodySize:N0} bytes this endpoint reads, so the server stopped reading it at that limit and refused the whole call. " +
             $"No call needs that much: {nameof(McpController.InspectBswupServiceWorker)}, the largest, reads at most " +
             $"{McpController.MaxScriptLength:N0} characters of script and {McpController.MaxAssetUrlsLength:N0} of asset URLs. " +
             "Pass the asset URLs in smaller batches."),

        BadHttpRequestException badRequest => (badRequest.StatusCode, badRequest.Message),

        _ => null
    };
}
