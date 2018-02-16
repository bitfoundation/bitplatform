using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Bit.ViewModel.Contracts
{
    public class Token
    {
        public static implicit operator Token(Account account)
        {
            return new Token
            {
                access_token = account.Properties["access_token"],
                expires_in = Convert.ToInt64(account.Properties["expires_in"]),
                login_date = Convert.ToDateTime(account.Properties["login_date"]),
                token_type = account.Properties["token_type"]
            };
        }

        public string access_token { get; set; }

        public string token_type { get; set; }

        public long expires_in { get; set; }

        public DateTimeOffset login_date { get; set; }
    }

    public interface ISecurityService
    {
        bool IsLoggedIn();

        Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task<Token> LoginWithCredentials(string username, string password, CancellationToken cancellationToken = default(CancellationToken));

        Task<Token> Login(object state = null, CancellationToken cancellationToken = default(CancellationToken));

        Token GetCurrentToken();

        Task<Token> GetCurrentTokenAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task Logout(CancellationToken cancellationToken = default(CancellationToken));
    }
}
