using Foundation.Api.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.Api.Contracts
{
    public interface IDefaultPageModelProvider
    {
        Task<DefaultPageModel> GetDefaultPageModelAsync(CancellationToken cancellationToken);

        DefaultPageModel GetDefaultPageModel();
    }
}
