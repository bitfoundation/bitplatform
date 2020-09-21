using Bit.Owin.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Contracts
{
    public interface IClientProfileModelProvider
    {
        Task<ClientProfileModel> GetClientProfileModelAsync(CancellationToken cancellationToken);

        ClientProfileModel GetClientProfileModel();
    }
}
