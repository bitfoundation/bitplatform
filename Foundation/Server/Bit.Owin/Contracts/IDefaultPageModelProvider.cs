using System.Threading;
using System.Threading.Tasks;
using Bit.Owin.Models;

namespace Bit.Owin.Contracts
{
    public interface IDefaultPageModelProvider
    {
        Task<DefaultPageModel> GetDefaultPageModelAsync(CancellationToken cancellationToken);

        DefaultPageModel GetDefaultPageModel();
    }
}
