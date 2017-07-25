using Bit.OData.ODataControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace Bit.OData.Implementations
{
    public class DefaultWebApiODataControllerActionSelector : ApiControllerActionSelector, IHttpActionSelector
    {
        private static readonly FieldInfo _cacheKeyField;

        private static readonly FieldInfo _actionNameFieldName;

        private static readonly TypeInfo cacheType;

        private static readonly FieldInfo _actionParameterNamesField;

        static DefaultWebApiODataControllerActionSelector()
        {
            _cacheKeyField = typeof(ApiControllerActionSelector).GetTypeInfo().GetField("_cacheKey", BindingFlags.NonPublic | BindingFlags.Instance);
            cacheType = typeof(ApiControllerActionSelector).GetTypeInfo().Assembly.GetType("System.Web.Http.Controllers.ApiControllerActionSelector+ActionSelectorCacheItem").GetTypeInfo();
            _actionParameterNamesField = cacheType.GetField("_actionParameterNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _actionNameFieldName = typeof(ReflectedHttpActionDescriptor).GetTypeInfo().GetField("_actionName", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private class HttpActionDescriptorAndKey
        {
            public HttpActionDescriptor HttpActionDescriptor;
            public string Key;
        }

        public override ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
        {
            ILookup<string, HttpActionDescriptor> actionMappings = base.GetActionMapping(controllerDescriptor);

            if (controllerDescriptor.Properties.Any() && !controllerDescriptor.Properties.TryGetValue("ApplyBitCRUDAttributesCalled", out object applyBitCRUDAttributesCalled))
            {
                object cacheKey = _cacheKeyField.GetValue(this);

                if (cacheKey != null && controllerDescriptor.Properties.TryGetValue(cacheKey, out object cache))
                {
                    IDictionary<HttpActionDescriptor, string[]> actions = (IDictionary<HttpActionDescriptor, string[]>)_actionParameterNamesField.GetValue(cache);

                    foreach (HttpActionDescriptor action in actions.Keys)
                    {
                        ApplyBitCRUDAttributes(action.ActionName, action);
                    }

                    controllerDescriptor.Properties.TryAdd("ApplyBitCRUDAttributesCalled", true);
                }
            }

            List<HttpActionDescriptorAndKey> actionMappingsList = new List<HttpActionDescriptorAndKey>();

            foreach (IGrouping<string, HttpActionDescriptor> aMaps in actionMappings)
            {
                foreach (HttpActionDescriptor httpActionDescriptor in aMaps)
                {
                    string actionName = ApplyBitCRUDAttributes(aMaps.Key, httpActionDescriptor);

                    actionMappingsList.Add(new HttpActionDescriptorAndKey
                    {
                        Key = actionName,
                        HttpActionDescriptor = httpActionDescriptor
                    });
                }
            }

            return actionMappingsList.ToLookup(httpActionDescriptorAndKey => httpActionDescriptorAndKey.Key, httpActionDescriptorAndKey => httpActionDescriptorAndKey.HttpActionDescriptor);
        }

        private string ApplyBitCRUDAttributes(string actionName, HttpActionDescriptor httpActionDescriptor)
        {
            string newActionName = actionName;

            IActionHttpMethodProvider actionHttpMethodProvider = httpActionDescriptor.GetCustomAttributes<IActionHttpMethodProvider>().FirstOrDefault();

            if (actionHttpMethodProvider != null)
            {
                if (actionHttpMethodProvider is GetAttribute)
                    newActionName = "Get";
                else if (actionHttpMethodProvider is PartialUpdateAttribute)
                {
                    newActionName = "Patch";
                    if (httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(void).GetTypeInfo() || httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(Task).GetTypeInfo())
                        throw new InvalidOperationException("Patch | PartialUpdate must have a return type other than Task or void");
                }
                else if (actionHttpMethodProvider is CreateAttribute)
                {
                    newActionName = "Post";
                    if (httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(void).GetTypeInfo() || httpActionDescriptor.ReturnType == typeof(Task).GetTypeInfo())
                        throw new InvalidOperationException("Post | Create must have a return type other than Task or void");
                }
                else if (actionHttpMethodProvider is UpdateAttribute)
                {
                    newActionName = "Put";
                    if (httpActionDescriptor.ReturnType.GetTypeInfo() == typeof(void).GetTypeInfo() || httpActionDescriptor.ReturnType == typeof(Task).GetTypeInfo())
                        throw new InvalidOperationException("Put | Update must have a return type other than Task or void");
                }
                else if (actionHttpMethodProvider is DeleteAttribute)
                    newActionName = "Delete";

                if (newActionName != actionName)
                    _actionNameFieldName.SetValue(httpActionDescriptor, newActionName);
            }

            return newActionName;
        }
    }
}
