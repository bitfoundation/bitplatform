using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Core.Implementations
{
    public class PreparedAppEnvironmentsProvider : IAppEnvironmentsProvider
    {
        private readonly AppEnvironment _appEnvironment;

        public PreparedAppEnvironmentsProvider(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            return _appEnvironment;
        }
    }
}
