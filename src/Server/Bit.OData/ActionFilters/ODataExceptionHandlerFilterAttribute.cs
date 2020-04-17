using Bit.Owin.Contracts;
using Bit.WebApi.ActionFilters;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData;
using System;
using System.Net.Http;
using System.Web.Http.Filters;

namespace Bit.OData.ActionFilters
{
    public class ODataExceptionHandlerFilterAttribute : ExceptionHandlerFilterAttribute
    {
        protected override HttpResponseMessage CreateErrorResponseMessage(HttpActionExecutedContext actionExecutedContext, IExceptionToHttpErrorMapper exceptionToHttpErrorMapper, Exception exception)
        {
            if (actionExecutedContext == null)
                throw new ArgumentNullException(nameof(actionExecutedContext));

            if (exceptionToHttpErrorMapper == null)
                throw new ArgumentNullException(nameof(exceptionToHttpErrorMapper));

            return actionExecutedContext.Request.CreateErrorResponse(exceptionToHttpErrorMapper.GetStatusCode(exception), new ODataError() { Message = exceptionToHttpErrorMapper.GetMessage(exception) });
        }
    }
}
