using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.OData.ActionFilters
{
    public class IgnoreODataEnableQueryAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext == null)
                throw new ArgumentNullException(nameof(actionContext));

            actionContext.Request.Properties["IgnoreODataEnableQuery"] = true;

            base.OnActionExecuting(actionContext);
        }
    }
}
