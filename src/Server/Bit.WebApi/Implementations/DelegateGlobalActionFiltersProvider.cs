using Bit.WebApi.Contracts;
using System;
using System.Web.Http;

namespace Bit.WebApi.Implementations
{
    public class DelegateGlobalActionFiltersProvider : IWebApiConfigurationCustomizer
    {
        protected DelegateGlobalActionFiltersProvider()
        {

        }

        private readonly Action<HttpConfiguration> _addGlobalActionFilters;

        public DelegateGlobalActionFiltersProvider(Action<HttpConfiguration> addGlobalActionFilters)
        {
            _addGlobalActionFilters = addGlobalActionFilters ?? throw new ArgumentNullException(nameof(addGlobalActionFilters));
        }

        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            _addGlobalActionFilters(webApiConfiguration);
        }
    }
}
