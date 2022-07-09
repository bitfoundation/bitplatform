﻿using System.Net;
using System.Reflection;

namespace Bit.Sales.WebSite.Api.Middlewares;

public class HttpResponseExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public HttpResponseExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IHostEnvironment webHostEnvironment)
    {
        // Using the Request-Id header, one can find the log for server-related exceptions
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
                // The details of all of the exceptions are returned only in dev mode. in any other modes like production, only the details of the known exceptions are returned.
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
    }

    private Exception UnWrapException(Exception exp)
    {
        return (exp is TargetInvocationException && exp.InnerException is not null)
            ? exp.InnerException
            : exp;
    }
}
