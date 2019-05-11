#if DotNet
using Bit.Owin.Contracts;
using System.Collections.Generic;
using System.Web.Routing;

namespace Bit.Owin.Implementations
{
    public class AspNetRouteValuesProvider : IRouteValuesProvider
    {
        public virtual IDictionary<string, object> PopulateRouteValuesDictionary(object routeValuesObj)
        {
            return new RouteValueDictionary(routeValuesObj);
        }
    }
}
#endif