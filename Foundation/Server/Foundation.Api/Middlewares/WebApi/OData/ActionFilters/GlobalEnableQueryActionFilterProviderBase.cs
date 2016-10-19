using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Query;

namespace Foundation.Api.Middlewares.WebApi.OData.ActionFilters
{
    public class GlobalEnableQueryActionFilterProviderBase : IWebApiGlobalActionFiltersProvider
    {
        public virtual void ConfigureGlobalActionFilter(HttpConfiguration webApiConfiguration)
        {
            ODataEnableQueryAttribute enableQueryOptions = new ODataEnableQueryAttribute
            {
                PageSize = 25,
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
