using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TodoTemplate.Api.Filters;

public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public HttpResponseExceptionFilter(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public int Order => int.MaxValue - 10;

    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        context.HttpContext.Response.Headers.Add("Request-ID", context.HttpContext.TraceIdentifier);

        if (context.Exception is not null)
        {
            var exception = UnWrapException(context.Exception);
            var isKnownException = exception is KnownException;
            var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);

            RestExceptionPayload restExceptionPayload = new RestExceptionPayload
            {
                Message = isKnownException || _webHostEnvironment.IsDevelopment() ? exception.Message : nameof(UnknownException),
                ExceptionType = isKnownException ? exception.GetType().FullName : typeof(UnknownException).FullName
            };

            if (exception is ResourceValidationException validationException)
            {
                restExceptionPayload.Details = validationException.Details;
            }

            context.Result = new JsonResult(restExceptionPayload)
            {
                StatusCode = statusCode
            };

            context.ExceptionHandled = true;
        }
    }

    Exception UnWrapException(Exception exp)
    {
        if (exp is TargetInvocationException)
            return exp.InnerException ?? exp;

        return exp;
    }
}
