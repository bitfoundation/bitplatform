using Microsoft.AspNet.OData.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;

namespace Bit.OData.Implementations
{
    public class DefaultODataHttpControllerSelector : DefaultHttpControllerSelector
    {
        private readonly HttpConfiguration _httpConfiguration;
        private readonly ICollection<Type> _controllerTypes;

        public DefaultODataHttpControllerSelector(HttpConfiguration httpConfiguration)
            : base(httpConfiguration)
        {
            _httpConfiguration = httpConfiguration;
            _controllerTypes = _httpConfiguration.Services.GetHttpControllerTypeResolver().GetControllerTypes(_httpConfiguration.Services.GetAssembliesResolver());
        }

        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            string controllerName = GetControllerName(request);
            string controllerFullName = $"{controllerName}{ControllerSuffix}";

            if (_controllerTypes.Count(t => t.Name == controllerFullName) > 1)
            {
                HttpRouteData httpRouteData = (HttpRouteData)request.GetRouteData();

                if (httpRouteData?.Route != null)
                {
                    string routePrefix = ((ODataRoute)httpRouteData.Route).RoutePrefix;

                    Type controllerType = _controllerTypes.ExtendedSingle($"Finding exact match controller for {controllerFullName}", t => t.Assembly.GetName().Name == routePrefix && t.Name == controllerFullName);

                    return new HttpControllerDescriptor(_httpConfiguration, controllerName, controllerType);
                }
            }

            return base.SelectController(request);
        }
    }
}
