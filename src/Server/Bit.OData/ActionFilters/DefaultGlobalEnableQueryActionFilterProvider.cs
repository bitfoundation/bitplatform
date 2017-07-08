using System.Web.Http;
using System.Web.OData.Extensions;
using System.Web.OData.Query;
using Bit.WebApi.Contracts;

namespace Bit.OData.ActionFilters
{
    public class DefaultGlobalEnableQueryActionFilterProvider : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
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
