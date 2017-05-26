using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Bit.Api.ApiControllers;

namespace Bit.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultWebApiODataControllerActionSelector : ApiControllerActionSelector, IHttpActionSelector
    {
        private readonly FieldInfo _actionNameFieldName = typeof(ReflectedHttpActionDescriptor).GetTypeInfo().GetField("_actionName",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private class HttpActionDescriptorAndKey
        {
            public HttpActionDescriptor HttpActionDescriptor;
            public string Key;
        }

        public override ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
        {
            ILookup<string, HttpActionDescriptor> actionMappings = base.GetActionMapping(controllerDescriptor);

            List<HttpActionDescriptorAndKey> actionMappingsList = new List<HttpActionDescriptorAndKey>();

            foreach (IGrouping<string, HttpActionDescriptor> aMaps in actionMappings)
            {
                foreach (HttpActionDescriptor httpActionDescriptor in aMaps)
                {
                    IActionHttpMethodProvider actionHttpMethodProvider =
                        httpActionDescriptor.GetCustomAttributes<IActionHttpMethodProvider>()
                        .FirstOrDefault();

                    string actionName = aMaps.Key;

                    if (actionHttpMethodProvider != null)
                    {
                        if (actionHttpMethodProvider is GetAttribute)
                            actionName = "Get";
                        else if (actionHttpMethodProvider is PartialUpdateAttribute)
                        {
                            actionName = "Patch";
                            if (httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(void).GetTypeInfo() || httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(Task).GetTypeInfo())
                                throw new InvalidOperationException("Patch | PartialUpdate must have a return type other than Task or void");
                        }
                        else if (actionHttpMethodProvider is CreateAttribute)
                        {
                            actionName = "Post";
                            if (httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(void).GetTypeInfo() || httpActionDescriptor.ReturnType == typeof(Task).GetTypeInfo())
                                throw new InvalidOperationException("Post | Create must have a return type other than Task or void");
                        }
                        else if (actionHttpMethodProvider is UpdateAttribute)
                        {
                            actionName = "Put";
                            if (httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(void).GetTypeInfo() || httpActionDescriptor.ReturnType == typeof(Task).GetTypeInfo())
                                throw new InvalidOperationException("Put | Update must have a return type other than Task or void");
                        }
                        else if (actionHttpMethodProvider is DeleteAttribute)
                            actionName = "Delete";

                        _actionNameFieldName.SetValue(httpActionDescriptor, actionName);
                    }

                    actionMappingsList.Add(new HttpActionDescriptorAndKey
                    {
                        Key = actionName,
                        HttpActionDescriptor = httpActionDescriptor
                    });
                }
            }

            return actionMappingsList.ToLookup(httpActionDescriptorAndKey => httpActionDescriptorAndKey.Key, httpActionDescriptorAndKey => httpActionDescriptorAndKey.HttpActionDescriptor);
        }

        public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            try
            {
                return base.SelectAction(controllerContext);
            }
            catch
            {
                object action = null;

                if (controllerContext?.RouteData?.Values?.TryGetValue("action", out action) == true)
                {
                    string actionName = Convert.ToString(action);

                    if (actionName == "Patch" || actionName == "PartialUpdate" || actionName == "Put")
                    {
                        if (actionName == "Patch")
                            actionName = "PartialUpdate";
                        else if (actionName == "Post")
                            actionName = "Create";
                        else if (actionName == "Put")
                            actionName = "Update";

                        controllerContext.RouteData.Values["action"] = actionName;

                        return base.SelectAction(controllerContext);
                    }
                }

                throw;
            }
        }
    }
}
