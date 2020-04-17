using System;
using System.Web.Http;
using Bit.WebApi.Contracts;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;

namespace Bit.OData.ActionFilters
{
    public class DefaultGlobalEnableQueryActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            ODataEnableQueryAttribute enableQueryOptions = new ODataEnableQueryAttribute
            {
                DefaultPageSize = null,
                MaxTop = int.MaxValue,
                MaxSkip = int.MaxValue,
                AllowedArithmeticOperators = AllowedArithmeticOperators.All,
                AllowedFunctions = AllowedFunctions.AllFunctions,
                AllowedLogicalOperators = AllowedLogicalOperators.All,
                AllowedQueryOptions = AllowedQueryOptions.All,
                HandleNullPropagation = HandleNullPropagationOption.False,
                EnsureStableOrdering = false,
                EnableConstantParameterization = false
            };

            webApiConfiguration.Filters.Add(enableQueryOptions);

            webApiConfiguration.AddODataQueryFilter(enableQueryOptions);

            webApiConfiguration.Count(QueryOptionSetting.Allowed);
            webApiConfiguration.Expand(QueryOptionSetting.Allowed);
            webApiConfiguration.Filter(QueryOptionSetting.Allowed);
            webApiConfiguration.OrderBy(QueryOptionSetting.Allowed);
            webApiConfiguration.Select(QueryOptionSetting.Allowed);
            webApiConfiguration.MaxTop(int.MaxValue);
        }
    }
}
