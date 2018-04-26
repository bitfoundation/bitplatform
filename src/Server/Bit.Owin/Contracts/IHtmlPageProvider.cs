using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Contracts
{
    public interface IHtmlPageProvider
    {
        Task<string> GetHtmlPageAsync(string htmlPage, CancellationToken cancellationToken);

        string GetIndexPageHtmlContents(string htmlPage);
    }
}
