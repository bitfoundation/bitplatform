using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Foundation.Core.Contracts;
using System.Linq;
using System.Web.OData.Query;
using System.Threading;
using Microsoft.Owin;

namespace Foundation.Api.Middlewares.WebApi.ActionFilters
{
    public class LogActionArgsFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            ILogger logger = actionContext.Request.GetOwinContext()
                .GetDependencyResolver()
                .Resolve<ILogger>();

            logger.AddLogData("ActionArgs", actionContext.ActionArguments.Where(arg => !(arg.Value is ODataQueryOptions) && !(arg.Value is CancellationToken)).ToArray());

            base.OnActionExecuting(actionContext);
        }
    }
}