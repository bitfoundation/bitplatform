using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Api.Contracts
{
    public interface ISsoPageHtmlProvider
    {
        string GetLoginViewModelName();

        string GetLoginViewPath();

        Task<string> GetSsoPageAsync(CancellationToken cancellationToken);
    }
}
