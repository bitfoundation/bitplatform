﻿using Bit.Websites.Sales.Api.Middlewares;

namespace Microsoft.AspNetCore.Builder;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHttpResponseExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpResponseExceptionHandlerMiddleware>();
    }
}
