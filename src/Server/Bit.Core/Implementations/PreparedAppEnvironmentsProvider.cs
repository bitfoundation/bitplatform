using Bit.Core.Contracts;
using Bit.Core.Models;
using System;

namespace Bit.Core.Implementations
{
    public class PreparedAppEnvironmentsProvider : IAppEnvironmentsProvider
    {
        private readonly AppEnvironment _appEnvironment;

        public PreparedAppEnvironmentsProvider(AppEnvironment appEnvironment)
        {
            if (appEnvironment == null)
                throw new ArgumentNullException(nameof(appEnvironment));

            appEnvironment.IsActive = true;

            _appEnvironment = appEnvironment;
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            return _appEnvironment;
        }

        public virtual (bool success, string message) TryGetActiveAppEnvironment(out AppEnvironment activeAppEnvironment)
        {
            activeAppEnvironment = _appEnvironment;
            return (true, null);
        }
    }
}
