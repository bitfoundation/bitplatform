using Foundation.Api.Contracts;
using Microsoft.Owin;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.Api.Implementations
{
    public class DefaultOwinActionFilterMiddleware : OwinMiddleware, IOwinActionFilter
    {
        public DefaultOwinActionFilterMiddleware()
            : base(null)
        {

        }

        public DefaultOwinActionFilterMiddleware(OwinMiddleware next)
            : base(next)
        {

        }

        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                await OnActionExecutingAsync(context, CancellationToken.None /*Get cancellation token from context*/);
                await Next?.Invoke(context);
            }
            catch (Exception ex)
            {
                await OnExceptionAsync(context, ex, CancellationToken.None /*Get cancellation token from context*/);
                throw;
            }
            finally
            {
                await OnActionExecutedAsync(context, CancellationToken.None /*Get cancellation token from context*/);
            }
        }

        public virtual async Task OnActionExecutedAsync(IOwinContext owinContext, CancellationToken cancellationToken)
        {

        }

        public virtual async Task OnActionExecutingAsync(IOwinContext owinContext, CancellationToken cancellationToken)
        {

        }

        public virtual async Task OnExceptionAsync(IOwinContext owinContext, Exception ex, CancellationToken cancellationToken)
        {

        }
    }
}
