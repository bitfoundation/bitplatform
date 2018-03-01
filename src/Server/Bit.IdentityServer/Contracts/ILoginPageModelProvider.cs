using System.Threading;
using System.Threading.Tasks;
using Bit.IdentityServer.Model;

namespace Bit.IdentityServer.Contracts
{
    public interface ILoginPageModelProvider
    {
        LoginPageModel GetLoginPageModel();

        Task<LoginPageModel> GetLoginPageModelAsync(CancellationToken cancellationToken);
    }
}
