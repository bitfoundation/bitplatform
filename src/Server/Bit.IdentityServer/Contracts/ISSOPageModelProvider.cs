using System.Threading;
using System.Threading.Tasks;
using Bit.IdentityServer.Model;

namespace Bit.IdentityServer.Contracts
{
    public interface ISSOPageModelProvider
    {
        SSOPageModel GetSSOPageModel();

        Task<SSOPageModel> GetSSOPageModelAsync(CancellationToken cancellationToken);
    }
}
