using Foundation.Api.Middlewares.WebApi.Contracts;
using System;
using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.Implementations
{
    public class DelegateGlobalActionFiltersProvider : IWebApiGlobalActionFiltersProvider
    {
        private readonly Action<HttpConfiguration> _addGlobalActionFilters;

        protected DelegateGlobalActionFiltersProvider()
        {

        }

        public DelegateGlobalActionFiltersProvider(Action<HttpConfiguration> addGlobalActionFilters)
        {
            if (addGlobalActionFilters == null)
                throw new ArgumentNullException(nameof(addGlobalActionFilters));

            _addGlobalActionFilters = addGlobalActionFilters;
        }

        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            _addGlobalActionFilters(webApiConfiguration);
        }
    }
}
