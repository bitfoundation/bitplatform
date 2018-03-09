using Bit.Core.Contracts;
using Bit.IdentityServer.Contracts;
using RazorEngine;
using RazorEngine.Templating;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class RazorLoginPageContentsProvider : ILoginPageContentsProvider
    {
        public virtual IDependencyManager DependencyResolver { get; set; }

        public virtual string GetLoginPageHtmlContents()
        {
            return Engine.Razor.Run("loginPage", typeof(IDependencyResolver), DependencyResolver);
        }

        public virtual Task<string> GetLoginPageHtmlContentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Engine.Razor.Run("loginPage", typeof(IDependencyResolver), DependencyResolver));
        }
    }
}
