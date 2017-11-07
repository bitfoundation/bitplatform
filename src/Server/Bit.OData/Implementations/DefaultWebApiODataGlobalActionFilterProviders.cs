using System.Collections.Generic;
using System.Web.Http;
using Bit.Core.Contracts;
using Bit.OData.ActionFilters;
using Bit.WebApi.Contracts;

namespace Bit.OData.Implementations
{
    public class GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual IEnumerable<IStringCorrector> StringCorrectors { get; set; }

        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestQSStringCorrectorsApplierActionFilterAttribute() { StringCorrectors = StringCorrectors });
        }
    }

    public class GlobalDefaultRequestQSTimeZoneApplierActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new RequestQSTimeZoneApplierActionFilterAttribute());
        }
    }

    public class GlobalODataNullReturnValueActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.Filters.Add(new ODataNullReturnValueActionFilter());
        }
    }
}
