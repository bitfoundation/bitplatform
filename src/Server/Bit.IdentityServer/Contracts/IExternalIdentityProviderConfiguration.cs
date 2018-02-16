using Owin;

namespace Bit.IdentityServer.Contracts
{
    public interface IExternalIdentityProviderConfiguration
    {
        void ConfiguerExternalIdentityProvider(IAppBuilder owinApp, string signInType);
    }
}
