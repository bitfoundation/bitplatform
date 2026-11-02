using Microsoft.AspNetCore.Mvc.Filters;
using ModelContextProtocol;

namespace Bit.BlazeDb.Demo.Server.Controllers;

/// <summary>
/// Gives a caller of the plain HTTP endpoints the reason a tool refused a request, as an MCP client gets it.
/// <para>
/// A tool throws <see cref="McpException"/> for a refusal meant for the caller - a source past the
/// inspector's limits, an inspection that ran out of time - because that is the one exception the MCP SDK
/// hands a client with its message. MVC knows nothing of it and would answer a bare 500, so here it becomes
/// a 400 whose problem details carry the same message.
/// </para>
/// </summary>
internal sealed class McpRefusalFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not McpException refusal) return;

        context.Result = new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request was refused.",
            Detail = refusal.Message
        });
        context.ExceptionHandled = true;
    }
}
