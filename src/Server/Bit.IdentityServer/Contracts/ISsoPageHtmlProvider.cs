using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Contracts
{
    public interface ISsoPageHtmlProvider
    {
        Task<string> GetSsoPageAsync(CancellationToken cancellationToken);
    }
}
