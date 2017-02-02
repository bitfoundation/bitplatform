using Foundation.Core.Models;

namespace Foundation.Core.Contracts
{
    public interface IAppEnvironmentProvider
    {
        AppEnvironment GetActiveAppEnvironment();
    }
}