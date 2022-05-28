using Bit.Core.Contracts;
using Bit.OData.ActionFilters;
using Bit.WebApi.Contracts;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Bit.OData.Implementations
{
    public class GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual IEnumerable<IStringCorrector> StringCorrectors { get; set; } = default!;

        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            webApiConfiguration.Filters.Add(new RequestQSStringCorrectorsApplierActionFilterAttribute() { StringCorrectors = StringCorrectors });
        }
    }

    public class SetODataSwaggerActionSelector : IWebApiConfigurationCustomizer
    {
        public void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            webApiConfiguration.Properties["SwaggerActionSelector"] = new DefaultWebApiODataControllerActionSelector();
        }
    }

    public class GlobalODataNullReturnValueActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            webApiConfiguration.Filters.Add(new ODataNullReturnValueActionFilterAttribute());
        }
    }
}
