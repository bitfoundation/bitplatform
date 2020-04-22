using Owin;

namespace Bit.IdentityServer.Contracts
{
    public interface IExternalIdentityProviderConfiguration
    {
        void ConfigureExternalIdentityProvider(IAppBuilder owinApp, string signInType);
    }
}
