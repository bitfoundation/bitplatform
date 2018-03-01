using System.Threading;
using System.Threading.Tasks;
using Bit.Owin.Models;

namespace Bit.Owin.Contracts
{
    public interface IIndexPageModelProvider
    {
        Task<IndexPageModel> GetIndexPageModelAsync(CancellationToken cancellationToken);

        IndexPageModel GetIndexPageModel();
    }
}
