using Bit.OData.Contracts;
using Microsoft.AspNet.OData.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;

namespace Bit.OData.Implementations
{
    public class DefaultODataHttpControllerSelector : DefaultHttpControllerSelector
    {
        private readonly HttpConfiguration _httpConfiguration;
        private readonly Dictionary<string, List<(string[] oDataRoutes, Type controllerType)>> _controllersGroupedByName;

        public DefaultODataHttpControllerSelector(HttpConfiguration httpConfiguration)
            : base(httpConfiguration)
        {
            if (httpConfiguration == null)
                throw new ArgumentNullException(nameof(httpConfiguration));

            _httpConfiguration = httpConfiguration;
            ICollection<Type> controllerTypes = _httpConfiguration.Services.GetHttpControllerTypeResolver().GetControllerTypes(_httpConfiguration.Services.GetAssembliesResolver());
            _controllersGroupedByName = controllerTypes.GroupBy(controller => controller.Name).ToDictionary(controller => controller.Key, controller => controller.Select(c => (c.Assembly.GetCustomAttributes<ODataModuleAttribute>().Select(oma => oma.ODataRouteName).ToArray(), c)).ToList());
        }

        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            string controllerName = GetControllerName(request);
            string controllerFullName = $"{controllerName}{ControllerSuffix}";

            if (_controllersGroupedByName.TryGetValue(controllerFullName, out List<(string[] oDataRoutes, Type controllerType)>? controllers) && controllers.Count > 1)
            {
                HttpRouteData httpRouteData = (HttpRouteData)request.GetRouteData();

                if (httpRouteData?.Route != null)
                {
                    string routePrefix = ((ODataRoute)httpRouteData.Route).RoutePrefix;

                    Type controllerType = controllers.ExtendedSingle($"Finding exact match controller for {controllerFullName}", t => t.oDataRoutes.Contains(routePrefix) && t.controllerType.Name == controllerFullName).controllerType;

                    return new HttpControllerDescriptor(_httpConfiguration, controllerName, controllerType);
                }
            }

            return base.SelectController(request);
        }
    }
}
