using IdentityServer3.Core.Models;

namespace Bit.IdentityServer.Contracts
{
    public interface ICustomLoginDataProvider
    {
        dynamic GetCustomData(LocalAuthenticationContext context);
    }
}
