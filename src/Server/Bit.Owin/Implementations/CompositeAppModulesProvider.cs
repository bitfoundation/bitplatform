using Bit.Core.Contracts;
using System;
using System.Collections.Generic;

namespace Bit.Owin.Implementations
{
    public class CompositeAppModulesProvider : IAppModulesProvider
    {
        protected CompositeAppModulesProvider()
        {

        }

        private readonly IAppModulesProvider[] _appModulesProviders;

        public CompositeAppModulesProvider(params IAppModulesProvider[] appModulesProviders)
        {
            _appModulesProviders = appModulesProviders ?? throw new ArgumentNullException(nameof(appModulesProviders));
        }

        public virtual IEnumerable<IAppModule> GetAppModules()
        {
            foreach (IAppModulesProvider appModulesProvider in _appModulesProviders)
            {
                foreach (IAppModule appModule in appModulesProvider.GetAppModules())
                {
                    yield return appModule;
                }
            }
        }
    }
}
