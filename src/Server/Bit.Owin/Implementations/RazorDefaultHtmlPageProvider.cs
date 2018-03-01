using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.Owin;
using RazorEngine;
using RazorEngine.Templating;

namespace Bit.Owin.Implementations
{
    public class RazorDefaultHtmlPageProvider : IIndexPageContentsProvider
    {
        public virtual IOwinContext OwinContext { get; set; }

        public virtual Task<string> GetIndexPageHtmlContentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Engine.Razor.Run("indexPage", typeof(IDependencyResolver),
                OwinContext.GetDependencyResolver()));
        }

        public virtual string GetIndexPageHtmlContents()
        {
            return Engine.Razor.Run("indexPage", typeof(IDependencyResolver),
                OwinContext.GetDependencyResolver());
        }
    }
}