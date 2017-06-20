using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Services.Default;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class PatternBasedRedirectUriValidator : DefaultRedirectUriValidator, IRedirectUriValidator
    {
        public override async Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            bool result = await base.IsPostLogoutRedirectUriValidAsync(requestedUri, client);

            if (result == false)
                return client.PostLogoutRedirectUris.Any(rUri => Regex.IsMatch(requestedUri, rUri, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant));

            return result;
        }

        public override async Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            bool result = await base.IsRedirectUriValidAsync(requestedUri, client);

            if (result == false)
                return client.RedirectUris.Any(rUri => Regex.IsMatch(requestedUri, rUri, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant));

            return result;
        }
    }
}
