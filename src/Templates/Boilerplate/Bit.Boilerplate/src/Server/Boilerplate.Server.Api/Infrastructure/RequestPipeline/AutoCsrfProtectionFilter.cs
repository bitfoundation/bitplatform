using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Boilerplate.Server.Api.Infrastructure.RequestPipeline;

/// <summary>
/// A global security filter that enforces "Secure by Default" protection against CSRF.
/// It automatically validates requests based on their authentication method and content type.
/// </summary>
/// <remarks>
/// <para>
/// The filter follows this logic precedence:
/// <list type="number">
/// <item><strong>Safe Methods:</strong> GET, HEAD, OPTIONS, TRACE are allowed (Read-only operations).</item>
/// <item><strong>Bearer Auth:</strong> Requests with an 'Authorization' header are allowed (Native/Hybrid clients).</item>
/// <item><strong>JSON Enforcement:</strong> Requests with 'application/json' are allowed (Relies on CORS preflight).</item>
/// <item><strong>Strict Fallback:</strong> All other requests (Forms, Multipart, etc.) MUST have a valid Anti-forgery token.</item>
/// </list>
/// </para>
/// </remarks>
public class AutoCsrfProtectionFilter(IAntiforgery antiforgery) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        // 1. Skip validation for idempotent (safe) methods.
        if (IsSafeMethod(request.Method))
        {
            await next();
            return;
        }

        // 2. Skip validation if the Authorization header is present.
        // Bearer tokens (JWT) are not automatically sent by browsers, making them CSRF-safe.
        if (request.Headers.ContainsKey("Authorization"))
        {
            await next();
            return;
        }

        // 3. Skip validation if the request is JSON.
        // Modern browsers enforce CORS preflight for JSON, which protects against cross-origin attacks.
        if (request.HasJsonContentType())
        {
            await next();
            return;
        }

        // 4. Manual Antiforgery Validation:
        // If the request is not a safe method, not using Header Auth, and not JSON (e.g., a Form submission),
        // it MUST contain a valid Anti-forgery token.
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult("Anti-forgery token validation failed. For non-JSON requests, a valid token is required.");
            return;
        }

        await next();
    }

    private static bool IsSafeMethod(string method)
    {
        return HttpMethods.IsGet(method) ||
               HttpMethods.IsHead(method) ||
               HttpMethods.IsOptions(method) ||
               HttpMethods.IsTrace(method);
    }
}
