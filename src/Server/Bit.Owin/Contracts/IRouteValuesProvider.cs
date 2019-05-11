using System.Collections.Generic;

namespace Bit.Owin.Contracts
{
    public interface IRouteValuesProvider
    {
        IDictionary<string, object> PopulateRouteValuesDictionary(object routeValuesObj);
    }
}
