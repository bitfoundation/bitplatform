using Bit.Core.Contracts;
using Microsoft.Owin;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.WebApi.ActionFilters
{
    public class LogActionArgsFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            ILogger logger = actionContext.Request.GetOwinContext()
                .GetDependencyResolver()
                .Resolve<ILogger>();

            logger.AddLogData("ActionArgs", actionContext.ActionArguments.Where(arg => LogParamter(arg.Value)).ToArray());

            base.OnActionExecuting(actionContext);
        }

        protected virtual bool LogParamter(object parameter)
        {
            return parameter != null && !(parameter is CancellationToken);
        }
    }
}