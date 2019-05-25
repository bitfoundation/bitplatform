using Bit.OData.ODataControllers;
using Microsoft.AspNet.OData.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace Bit.OData.Implementations
{
    public class DefaultWebApiODataControllerActionSelector : IHttpActionSelector
    {
        private readonly ApiControllerActionSelector _webApiControllerActionSelector = new ApiControllerActionSelector();

        protected virtual string GetActionName(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            IActionHttpMethodProvider actionHttpMethodProvider = method.GetCustomAttributes().OfType<IActionHttpMethodProvider>()
                .ExtendedSingle($"Finding IActionHttpMethodProvider on method {method.Name} on type {method.DeclaringType?.Name}");

            if (actionHttpMethodProvider is FunctionAttribute)
                return method.Name;
            else if (actionHttpMethodProvider is ActionAttribute)
                return method.Name;
            else if (actionHttpMethodProvider is GetAttribute)
                return nameof(HttpMethod.Get);
            else if (actionHttpMethodProvider is CreateAttribute)
                return nameof(HttpMethod.Post);
            else if (actionHttpMethodProvider is UpdateAttribute)
                return nameof(HttpMethod.Put);
            else if (actionHttpMethodProvider is PartialUpdateAttribute)
                return "Patch";
            else if (actionHttpMethodProvider is DeleteAttribute)
                return nameof(HttpMethod.Delete);

            throw new InvalidOperationException($"Invalid actionHttpMethodProvider type: {actionHttpMethodProvider.GetType().Name}");
        }

        public virtual ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
        {
            if (!controllerDescriptor.ControllerType.GetTypeInfo().IsDtoController())
                return _webApiControllerActionSelector.GetActionMapping(controllerDescriptor);

            if (!controllerDescriptor.Properties.TryGetValue("CachedActions", out object cachedActions))
            {
                List<MethodInfo> allActions = controllerDescriptor.ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttributes().OfType<IActionHttpMethodProvider>().Any())
                    .ToList();

                ILookup<string, HttpActionDescriptor> actionsLookup = allActions.ToLookup(GetActionName, method => (HttpActionDescriptor)(new ReflectedHttpActionDescriptor(controllerDescriptor, method)));

                controllerDescriptor.Properties.TryAdd("CachedActions", actionsLookup);
                controllerDescriptor.Properties.TryAdd("CachedActionsList", actionsLookup.SelectMany(actions => actions).Cast<ReflectedHttpActionDescriptor>().ToList());

                return actionsLookup;
            }
            else
            {
                return (ILookup<string, HttpActionDescriptor>)cachedActions;
            }
        }

        public virtual HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            if (!controllerContext.ControllerDescriptor.ControllerType.GetTypeInfo().IsDtoController())
                return _webApiControllerActionSelector.SelectAction(controllerContext);

            List<ReflectedHttpActionDescriptor> allActions = (List<ReflectedHttpActionDescriptor>)controllerContext.ControllerDescriptor.Properties["CachedActionsList"];

            IDictionary<string, object> routeData = ((HttpRouteData)controllerContext.RouteData).Values;
            routeData.TryGetValue("action", out object actionNameObj);
            string actionName = Convert.ToString(actionNameObj, CultureInfo.InvariantCulture);

            HttpActionDescriptor resultAction = null;

            if (string.Equals(actionName, HttpMethod.Get.Method, StringComparison.InvariantCultureIgnoreCase) && controllerContext.Request.Method == HttpMethod.Get)
            {
                if (routeData.ContainsKey("key")) // get by key
                    resultAction = allActions.ExtendedSingleOrDefault($"Finding Get by key action in {controllerContext.ControllerDescriptor.ControllerName} controller", action => action.MethodInfo.GetCustomAttribute<GetAttribute>() != null && action.MethodInfo.GetParameters().Any(p => p.Name == "key"));
                else // get all
                    resultAction = allActions.ExtendedSingleOrDefault($"Finding Get all action in {controllerContext.ControllerDescriptor.ControllerName} controller", action => action.MethodInfo.GetCustomAttribute<GetAttribute>() != null && action.MethodInfo.GetParameters().All(p => typeof(CancellationToken).IsAssignableFrom(p.ParameterType) || typeof(ODataQueryOptions).IsAssignableFrom(p.ParameterType)));
            }
            else if (string.Equals(actionName, HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase) && controllerContext.Request.Method == HttpMethod.Post)
                resultAction = allActions.ExtendedSingleOrDefault($"Finding create action in {controllerContext.ControllerDescriptor.ControllerName} controller", action => action.MethodInfo.GetCustomAttribute<CreateAttribute>() != null);
            else if (string.Equals(actionName, HttpMethod.Put.Method, StringComparison.InvariantCultureIgnoreCase) && controllerContext.Request.Method == HttpMethod.Put)
                resultAction = allActions.ExtendedSingleOrDefault($"Finding update action in {controllerContext.ControllerDescriptor.ControllerName} controller", action => action.MethodInfo.GetCustomAttribute<UpdateAttribute>() != null);
            else if (string.Equals(actionName, "Patch", StringComparison.InvariantCultureIgnoreCase) && controllerContext.Request.Method == new HttpMethod("Patch"))
                resultAction = allActions.ExtendedSingleOrDefault($"Finding partial update action in {controllerContext.ControllerDescriptor.ControllerName} controller", action => action.MethodInfo.GetCustomAttribute<PartialUpdateAttribute>() != null);
            else if (string.Equals(actionName, HttpMethod.Delete.Method, StringComparison.InvariantCultureIgnoreCase) && controllerContext.Request.Method == HttpMethod.Delete)
                resultAction = allActions.ExtendedSingleOrDefault($"Finding delete action in {controllerContext.ControllerDescriptor.ControllerName} controller", action => action.MethodInfo.GetCustomAttribute<DeleteAttribute>() != null);
            else
                resultAction = allActions.ExtendedSingleOrDefault($"Finding odata action/function for {actionName} in {controllerContext.ControllerDescriptor.ControllerName} controller", action => string.Equals(action.MethodInfo.Name, actionName, StringComparison.InvariantCultureIgnoreCase));

            return resultAction;
        }
    }
}
