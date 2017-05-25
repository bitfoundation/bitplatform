using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Contracts
{
    public interface IDefaultHtmlPageProvider
    {
        Task<string> GetDefaultPageAsync(CancellationToken cancellationToken);

        string GetDefaultPage();
    }
}
