using System.Threading;
using System.Threading.Tasks;
using Bit.Owin.Models;

namespace Bit.Owin.Contracts
{
    public interface IClientProfileModelProvider
    {
        Task<ClientProfileModel> GetClientProfileModelAsync(CancellationToken cancellationToken);

        ClientProfileModel GetClientProfileModel();
    }
}
