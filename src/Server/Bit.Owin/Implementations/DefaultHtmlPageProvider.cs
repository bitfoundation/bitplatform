using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class DefaultHtmlPageProvider : IHtmlPageProvider
    {
        public virtual IClientProfileModelProvider ClientProfileModelProvider { get; set; }

        public virtual async Task<string> GetHtmlPageAsync(string htmlPage, CancellationToken cancellationToken)
        {
            ClientProfileModel clientProfileModel = await ClientProfileModelProvider.GetClientProfileModelAsync(cancellationToken);

            return ReplaceDefinedVairables(htmlPage, clientProfileModel);
        }

        private string ReplaceDefinedVairables(string htmlPage, ClientProfileModel clientProfileModel)
        {
            return htmlPage
                                .Replace("{{model.AppVersion}}", clientProfileModel.AppVersion)
                                .Replace("{{model.AppName}}", clientProfileModel.AppName)
                                .Replace("{{model.DebugMode}}", clientProfileModel.DebugMode.ToString().ToLowerInvariant())
                                .Replace("{{model.BaseHref}}", clientProfileModel.BaseHref)
                                .Replace("{{model.Title}}", clientProfileModel.AppTitle)
                                .Replace("{{model.Theme}}", clientProfileModel.Theme)
                                .Replace("{{model.Culture}}", clientProfileModel.Culture)
                                .Replace("{{model.TimeZone}}", clientProfileModel.DesiredTimeZoneValue)
                                .Replace("<script src=\"ClientAppProfile\" type=\"text/javascript\"></script>", clientProfileModel.ToJavaScriptTag());
        }

        public virtual string GetIndexPageHtmlContents(string htmlPage)
        {
            ClientProfileModel clientProfileModel = ClientProfileModelProvider.GetClientProfileModel();

            return ReplaceDefinedVairables(htmlPage, clientProfileModel);
        }
    }
}