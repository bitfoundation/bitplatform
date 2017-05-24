using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Api.Contracts
{
    public interface ISsoPageHtmlProvider
    {
        Task<string> GetSsoPageAsync(CancellationToken cancellationToken);
    }
}
