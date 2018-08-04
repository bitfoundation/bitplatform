using Bit.Core.Models;

namespace Bit.Core.Contracts
{
    /// <summary>
    /// Provides active app environment (<see cref="AppEnvironment"/>).
    /// </summary>
    public interface IAppEnvironmentsProvider
    {
        /// <summary>
        /// Return active app environment. An <see cref="AppEnvironment"/> with <see cref="AppEnvironment.IsActive"/> equals true.
        /// </summary>
        AppEnvironment GetActiveAppEnvironment();
    }
}
