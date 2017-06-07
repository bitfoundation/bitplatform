using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.OData.Query;
using Bit.Core.Contracts;
using Microsoft.Owin;

namespace Bit.Api.Middlewares.WebApi.ActionFilters
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