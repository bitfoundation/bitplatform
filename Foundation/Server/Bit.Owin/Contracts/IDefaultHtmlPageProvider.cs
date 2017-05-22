using System.Threading;
using System.Threading.Tasks;

namespace Foundation.Api.Contracts
{
    public interface IDefaultHtmlPageProvider
    {
        Task<string> GetDefaultPageAsync(CancellationToken cancellationToken);

        string GetDefaultPage();
    }
}
