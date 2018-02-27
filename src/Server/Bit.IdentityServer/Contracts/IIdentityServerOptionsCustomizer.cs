using IdentityServer3.Core.Configuration;

namespace Bit.IdentityServer.Contracts
{
    public interface IIdentityServerOptionsCustomizer
    {
        void Customize(IdentityServerOptions identityServerOptions);
    }
}
