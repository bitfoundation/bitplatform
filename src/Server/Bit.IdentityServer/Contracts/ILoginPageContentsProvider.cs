using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Contracts
{
    public interface ILoginPageContentsProvider
    {
        Task<string> GetLoginPageHtmlContentsAsync(CancellationToken cancellationToken);

        string GetLoginPageHtmlContents();
    }
}
