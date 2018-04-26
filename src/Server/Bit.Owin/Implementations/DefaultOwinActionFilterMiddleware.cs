using System;
using System.Threading.Tasks;
using Bit.Owin.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Implementations
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
                await OnActionExecutingAsync(context);
                if (Next != null)
                    await Next.Invoke(context);
            }
            catch (Exception ex)
            {
                await OnExceptionAsync(context, ex);
                throw;
            }
            finally
            {
                await OnActionExecutedAsync(context);
            }
        }

        public virtual Task OnActionExecutedAsync(IOwinContext owinContext)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnActionExecutingAsync(IOwinContext owinContext)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnExceptionAsync(IOwinContext owinContext, Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}
