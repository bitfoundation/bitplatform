using Bit.Core.Models;

namespace Bit.Core.Contracts
{
    public interface IAppEnvironmentProvider
    {
        AppEnvironment GetActiveAppEnvironment();
    }
}