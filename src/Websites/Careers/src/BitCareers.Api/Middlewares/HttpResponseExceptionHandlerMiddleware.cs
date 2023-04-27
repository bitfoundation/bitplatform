﻿using System.Net;
using System.Reflection;
using BitCareers.Shared.Exceptions;

namespace BitCareers.Api.Middlewares;

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
            var knownException = exception as KnownException;

            // The details of all of the exceptions are returned only in dev mode. in any other modes like production, only the details of the known exceptions are returned.
            string key = knownException?.Key ?? nameof(UnknownException);
            string message =  exception.Message;

            var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);

            RestErrorInfo restExceptionPayload = new RestErrorInfo
            {
                Key = key,
                Message = message,
                ExceptionType = knownException?.GetType().FullName ?? typeof(UnknownException).FullName
            };

            if (exception is ResourceValidationException validationException)
            {
                restExceptionPayload.Payload = validationException.Payload;
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
