using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.WebApi.ActionFilters
{
    /// <summary>
    /// See https://github.com/odata/odata.net/issues/165
    /// </summary>
    public class AddAcceptCharsetToRequestHeadersIfNotAnyFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.Request.Headers.Contains("Accept-Charset"))
                actionContext.Request.Headers.Add("Accept-Charset", new[] { "utf-8" });

            base.OnActionExecuting(actionContext);
        }
    }
}
