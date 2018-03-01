using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Contracts
{
    public interface IIndexPageContentsProvider
    {
        Task<string> GetIndexPageHtmlContentsAsync(CancellationToken cancellationToken);

        string GetIndexPageHtmlContents();
    }
}
