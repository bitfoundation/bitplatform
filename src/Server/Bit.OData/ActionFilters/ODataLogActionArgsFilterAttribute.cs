using Bit.WebApi.ActionFilters;
using System.Web.OData.Query;

namespace Bit.OData.ActionFilters
{
    public class ODataLogActionArgsFilterAttribute : LogActionArgsFilterAttribute
    {
        protected override bool LogParameter(object parameter)
        {
            return base.LogParameter(parameter) && !(parameter is ODataQueryOptions);
        }
    }
}
