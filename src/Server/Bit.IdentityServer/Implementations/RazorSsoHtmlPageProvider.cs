using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.IdentityServer.Contracts;
using RazorEngine;
using RazorEngine.Templating;

namespace Bit.IdentityServer.Implementations
{
    public class RazorSsoHtmlPageProvider : ISsoPageHtmlProvider
    {
        public virtual IDependencyManager DependencyResolver { get; set; }

        public virtual async Task<string> GetSsoPageAsync(CancellationToken cancellationToken)
        {
            return Engine.Razor.Run("ssoPageTemplate", typeof(IDependencyResolver), DependencyResolver);
        }
    }
}
