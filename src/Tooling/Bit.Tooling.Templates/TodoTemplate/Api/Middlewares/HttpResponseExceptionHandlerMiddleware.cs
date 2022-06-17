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
            
            var restExceptionPayload = RestExceptionPayloadBuilder(webHostEnvironment, isKnownException, exception);

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(restExceptionPayload);
        }
    }

    private RestExceptionPayload RestExceptionPayloadBuilder(IHostEnvironment webHostEnvironment, bool isKnownException, Exception exception)
    {
        RestExceptionPayload restExceptionPayload = new RestExceptionPayload
        {
            Message = isKnownException || webHostEnvironment.IsDevelopment() ? exception.Message : nameof(UnknownException),
            ExceptionType = isKnownException ? exception.GetType().FullName : typeof(UnknownException).FullName
        };

        if (exception is ResourceValidationException validationException)
        {
            restExceptionPayload.Details = validationException.Details;
        }

        return restExceptionPayload;
    }

    private Exception UnWrapException(Exception exp)
    {
        return (exp is TargetInvocationException && exp.InnerException is not null)
            ? exp.InnerException
            : exp;
    }
}
