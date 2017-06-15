using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.Api.Middlewares.WebApi.OData.ActionFilters
{
    public class IgnoreODataEnableQueryAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            actionContext.Request.Properties["IgnoreODataEnableQuery"] = true;

            base.OnActionExecuting(actionContext);
        }
    }
}
