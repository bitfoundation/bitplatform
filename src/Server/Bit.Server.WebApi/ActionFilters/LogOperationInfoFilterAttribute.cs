using Bit.Core.Contracts;
using Microsoft.Owin;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.WebApi.ActionFilters
{
    public class LogOperationInfoFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext == null)
                throw new ArgumentNullException(nameof(actionContext));

            ILogger logger = actionContext.Request.GetOwinContext()
                .GetDependencyResolver()
                .Resolve<ILogger>();

            logger.AddLogData("OperationArgs", actionContext.ActionArguments.Where(arg => LogParameter(arg.Value)).ToArray());
            logger.AddLogData("OperationName", actionContext.ActionDescriptor.ActionName);
            logger.AddLogData("ControllerName", actionContext.ControllerContext.ControllerDescriptor.ControllerName);

            base.OnActionExecuting(actionContext);
        }

        protected virtual bool LogParameter(object? parameter)
        {
            return parameter != null && !(parameter is CancellationToken);
        }
    }
}