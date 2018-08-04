using Microsoft.Owin;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Contracts
{
    public interface IOwinActionFilter
    {
        Task OnActionExecutingAsync(IOwinContext owinContext);

        Task OnActionExecutedAsync(IOwinContext owinContext);

        Task OnExceptionAsync(IOwinContext owinContext, Exception ex);
    }
}
