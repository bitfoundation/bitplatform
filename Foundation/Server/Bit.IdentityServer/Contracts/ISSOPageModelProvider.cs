using IdentityServer.Api.Model;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Api.Contracts
{
    public interface ISSOPageModelProvider
    {
        SSOPageModel GetSSOPageModel();

        Task<SSOPageModel> GetSSOPageModelAsync(CancellationToken cancellationToken);
    }
}
