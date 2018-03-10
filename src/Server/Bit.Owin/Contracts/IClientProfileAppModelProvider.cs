using System.Threading;
using System.Threading.Tasks;
using Bit.Owin.Models;

namespace Bit.Owin.Contracts
{
    public interface IClientProfileAppModelProvider
    {
        Task<ClientAppProfileModel> GetClientAppProfileModelAsync(CancellationToken cancellationToken);

        ClientAppProfileModel GetClientAppProfileModel();
    }
}
