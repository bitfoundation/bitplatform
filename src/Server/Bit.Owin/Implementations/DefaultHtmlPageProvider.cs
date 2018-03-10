using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class DefaultHtmlPageProvider : IHtmlPageProvider
    {
        private IAppEnvironmentProvider _appEnvironmentProvider;
        protected AppEnvironment ActiveAppEnvironment { get; set; }

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            get { return _appEnvironmentProvider; }
            set
            {
                _appEnvironmentProvider = value;
                ActiveAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();
            }
        }

        public virtual Task<string> GetHtmlPageAsync(string htmlPage, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProduceFinalHtml(htmlPage));
        }

        private string ProduceFinalHtml(string htmlPage)
        {
            return htmlPage
                    .Replace("{{model.AppVersion}}", ActiveAppEnvironment.AppInfo.Version)
                    .Replace("{{model.BaseHref}}", ActiveAppEnvironment.GetHostVirtualPath());
        }

        public virtual string GetIndexPageHtmlContents(string htmlPage)
        {
            return ProduceFinalHtml(htmlPage);
        }
    }
}