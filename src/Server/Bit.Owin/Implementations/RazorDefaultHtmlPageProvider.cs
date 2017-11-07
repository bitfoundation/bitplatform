using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.Owin;
using RazorEngine;
using RazorEngine.Templating;

namespace Bit.Owin.Implementations
{
    public class RazorDefaultHtmlPageProvider : IDefaultHtmlPageProvider
    {
        public virtual IOwinContext OwinContext { get; set; }

        public virtual Task<string> GetDefaultPageAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Engine.Razor.Run("defaultPageTemplate", typeof(IDependencyResolver),
                OwinContext.GetDependencyResolver()));
        }

        public virtual string GetDefaultPage()
        {
            return Engine.Razor.Run("defaultPageTemplate", typeof(IDependencyResolver),
                OwinContext.GetDependencyResolver());
        }
    }
}