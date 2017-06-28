using Bit.WebApi.ActionFilters;
using System.Web.OData.Query;

namespace Bit.OData.ActionFilters
{
    public class ODataLogActionArgsFilterAttribute : LogActionArgsFilterAttribute
    {
        protected override bool LogParamter(object parameter)
        {
            return base.LogParamter(parameter) && !(parameter is ODataQueryOptions);
        }
    }
}
