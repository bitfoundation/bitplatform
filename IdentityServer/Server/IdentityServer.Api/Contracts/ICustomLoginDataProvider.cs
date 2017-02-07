using IdentityServer3.Core.Models;

namespace IdentityServer.Api.Contracts
{
    public interface ICustomLoginDataProvider
    {
        dynamic GetCustomData(LocalAuthenticationContext context);
    }
}
