using Bit.Owin.Contracts;
using Bit.Owin.Models;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class DefaultHtmlPageProvider : IHtmlPageProvider
    {
        public virtual IClientProfileModelProvider ClientProfileModelProvider { get; set; } = default!;

        public virtual async Task<string> GetHtmlPageAsync(string htmlPage, CancellationToken cancellationToken)
        {
            if (htmlPage == null)
                throw new ArgumentNullException(nameof(htmlPage));

            ClientProfileModel clientProfileModel = await ClientProfileModelProvider.GetClientProfileModelAsync(cancellationToken).ConfigureAwait(false);

            return ReplaceDefinedVariables(htmlPage, clientProfileModel);
        }

        string ReplaceDefinedVariables(string htmlPage, ClientProfileModel clientProfileModel)
        {
            return htmlPage
                                .Replace("{{model.AppVersion}}", clientProfileModel.AppVersion, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{{model.AppName}}", clientProfileModel.AppName, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{{model.DebugMode}}", clientProfileModel.DebugMode.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{{model.BaseHref}}", clientProfileModel.BaseHref, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{{model.Title}}", clientProfileModel.AppTitle, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{{model.Theme}}", clientProfileModel.Theme, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{{model.Culture}}", clientProfileModel.Culture, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{{model.TimeZone}}", clientProfileModel.DesiredTimeZoneValue, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("<script src=\"ClientAppProfile\" type=\"text/javascript\"></script>", clientProfileModel.ToJavaScriptTag(), StringComparison.InvariantCultureIgnoreCase);
        }

        public virtual string GetIndexPageHtmlContents(string htmlPage)
        {
            if (htmlPage == null)
                throw new ArgumentNullException(nameof(htmlPage));

            ClientProfileModel clientProfileModel = ClientProfileModelProvider.GetClientProfileModel();

            return ReplaceDefinedVariables(htmlPage, clientProfileModel);
        }
    }
}
