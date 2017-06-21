using Bit.Core.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Services.Default;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bit.Core.Models;

namespace Bit.IdentityServer.Implementations
{
    public class ExtendedRedirectUriValidator : DefaultRedirectUriValidator, IRedirectUriValidator
    {
        private readonly AppEnvironment _activeAppEnvironment;

        public ExtendedRedirectUriValidator(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        protected ExtendedRedirectUriValidator()
        {

        }

        public override async Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            return _activeAppEnvironment.DebugMode == true ||
                (await base.IsPostLogoutRedirectUriValidAsync(requestedUri, client)) ||
                client.PostLogoutRedirectUris.Any(rUri => Regex.IsMatch(requestedUri, rUri, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant));
        }

        public override async Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            return _activeAppEnvironment.DebugMode == true ||
                (await base.IsRedirectUriValidAsync(requestedUri, client)) ||
                client.RedirectUris.Any(rUri => Regex.IsMatch(requestedUri, rUri, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant));
        }
    }
}
