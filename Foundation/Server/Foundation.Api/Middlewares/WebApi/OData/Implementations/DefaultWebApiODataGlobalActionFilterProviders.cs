using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Api.Middlewares.WebApi.OData.ActionFilters;
using Foundation.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.OData.Implementations
{
    public class GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider : IWebApiGlobalActionFiltersProvider
    {
        private readonly IEnumerable<IStringCorrector> _stringCorrectors;

        public GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider(IEnumerable<IStringCorrector> stringCorrectors)
        {
            if (stringCorrectors == null)
                throw new ArgumentNullException(nameof(stringCorrectors));

            _stringCorrectors = stringCorrectors;
        }

        protected GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider()
        {

        }

        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestQSStringCorrectorsApplierActionFilterAttribute(_stringCorrectors));
        }
    }

    public class GlobalDefaultRequestQSTimeZoneApplierActionFilterProvider : IWebApiGlobalActionFiltersProvider
    {
        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestQSTimeZoneApplierActionFilterAttribute());
        }
    }
}
