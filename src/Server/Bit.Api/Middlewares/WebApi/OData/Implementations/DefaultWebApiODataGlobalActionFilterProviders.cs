using System;
using System.Collections.Generic;
using System.Web.Http;
using Bit.Api.Middlewares.WebApi.Contracts;
using Bit.Api.Middlewares.WebApi.OData.ActionFilters;
using Bit.Core.Contracts;

namespace Bit.Api.Middlewares.WebApi.OData.Implementations
{
    public class GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider : IWebApiConfigurationCustomizer
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

        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestQSStringCorrectorsApplierActionFilterAttribute(_stringCorrectors));
        }
    }

    public class GlobalDefaultRequestQSTimeZoneApplierActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestQSTimeZoneApplierActionFilterAttribute());
        }
    }
}
