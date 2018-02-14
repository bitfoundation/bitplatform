using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel.Contracts
{
    public interface ISecurityService
    {
        Task<bool> IsLoggedIn(CancellationToken cancellationToken = default(CancellationToken));

        Task<(string access_token, long expires_in, string token_type)> LoginWithCredentials(string username, string password, CancellationToken cancellationToken = default(CancellationToken));

        Task<(string access_token, long expires_in, string token_type)> Login(object state = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<(string access_token, long expires_in, string token_type)> GetCurrentToken(CancellationToken cancellationToken = default(CancellationToken));

        Task Logout(CancellationToken cancellationToken = default(CancellationToken));
    }
}
