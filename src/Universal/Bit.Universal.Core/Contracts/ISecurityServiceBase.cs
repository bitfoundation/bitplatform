using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    public interface ISecurityServiceBase
    {
        Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default);

        bool IsLoggedIn();

        Task<string?> GetUserIdAsync(CancellationToken cancellationToken);

        Task Logout(object? state = null, string? client_id = null, CancellationToken cancellationToken = default);

        Uri GetLoginUrl(object? state = null, string? client_id = null, IDictionary<string, string?>? acr_values = null);

        Uri GetLogoutUrl(string id_token, object? state = null, string? client_id = null);

        bool UseSecureStorage();
    }
}
