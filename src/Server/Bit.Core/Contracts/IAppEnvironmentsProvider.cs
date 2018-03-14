using Bit.Core.Models;

namespace Bit.Core.Contracts
{
    public interface IAppEnvironmentsProvider
    {
        AppEnvironment GetActiveAppEnvironment();
    }
}