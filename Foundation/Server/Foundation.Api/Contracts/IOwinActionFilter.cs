using Microsoft.Owin;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.Api.Contracts
{
    public interface IOwinActionFilter
    {
        Task OnActionExecutingAsync(IOwinContext owinContext, CancellationToken cancellationToken);

        Task OnActionExecutedAsync(IOwinContext owinContext, CancellationToken cancellationToken);

        Task OnExceptionAsync(IOwinContext owinContext, Exception ex, CancellationToken cancellationToken);
    }
}
