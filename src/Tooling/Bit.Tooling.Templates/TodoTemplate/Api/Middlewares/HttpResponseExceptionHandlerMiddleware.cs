using System.Net;
using System.Reflection;

namespace TodoTemplate.Api.Middlewares;

public class HttpResponseExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public HttpResponseExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IHostEnvironment webHostEnvironment)
    {
        context.Response.Headers.Add("Request-ID", context.TraceIdentifier);

        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            var exception = UnWrapException(e);
            var isKnownException = exception is KnownException;
            var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);
            
            RestExceptionPayload restExceptionPayload = new RestExceptionPayload
            {
                Message = isKnownException || webHostEnvironment.IsDevelopment() ? exception.Message : nameof(UnknownException),
                ExceptionType = isKnownException ? exception.GetType().FullName : typeof(UnknownException).FullName
            };

            if (exception is ResourceValidationException validationException)
            {
                restExceptionPayload.Details = validationException.Details;
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(restExceptionPayload);
        }

        Exception UnWrapException(Exception exp)
        {
            if (exp is TargetInvocationException)
                return exp.InnerException ?? exp;

            return exp;
        }
    }
}

public static class HttpResponseExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpResponseExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpResponseExceptionHandlerMiddleware>();
    }
}
