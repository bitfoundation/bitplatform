using Bit.Owin.Contracts;
using Bit.WebApi.ActionFilters;
using Microsoft.OData;
using System;
using System.Net.Http;
using System.Web.Http.Filters;
using System.Web.OData.Extensions;

namespace Bit.OData.ActionFilters
{
    public class ODataExceptionHandlerFilterAttribute : ExceptionHandlerFilterAttribute
    {
        protected override HttpResponseMessage CreateErrorResponseMessage(HttpActionExecutedContext actionExecutedContext, IExceptionToHttpErrorMapper exceptionToHttpErrorMapper, Exception exception)
        {
            return actionExecutedContext.Request.CreateErrorResponse(exceptionToHttpErrorMapper.GetStatusCode(exception), new ODataError() { Message = exceptionToHttpErrorMapper.GetMessage(exception) });
        }
    }
}
