using Bit.WebApi.ActionFilters;
using Microsoft.AspNet.OData.Query;

namespace Bit.OData.ActionFilters
{
    public class ODataLogOperationInfoFilterAttribute : LogOperationInfoFilterAttribute
    {
        protected override bool LogParameter(object? parameter)
        {
            return base.LogParameter(parameter) && !(parameter is ODataQueryOptions);
        }
    }
}
