using Bit.OData.ODataControllers;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using System;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;

namespace Bit.OData.Serialization
{
    public class ExtendedODataDeserializerProvider : DefaultODataDeserializerProvider
    {
        protected ExtendedODataDeserializerProvider()
            : base(null)
        {
        }

        public ExtendedODataDeserializerProvider(IServiceProvider rootContainer)
            : base(rootContainer)
        {
            _defaultODataParameterDeserializerValue = new Lazy<DefaultODataActionCreateUpdateParameterDeserializer>(() => (DefaultODataActionCreateUpdateParameterDeserializer)rootContainer.GetService(typeof(DefaultODataActionCreateUpdateParameterDeserializer).GetTypeInfo()), isThreadSafe: true);
        }

        private readonly Lazy<DefaultODataActionCreateUpdateParameterDeserializer> _defaultODataParameterDeserializerValue;

        public override ODataDeserializer GetODataDeserializer(Type type, HttpRequestMessage request)
        {
            HttpActionDescriptor actionDescriptor = request.GetActionDescriptor();

            if (actionDescriptor != null && (actionDescriptor.GetCustomAttributes<ActionAttribute>().Count > 0
                || actionDescriptor.GetCustomAttributes<CreateAttribute>().Count > 0
                || actionDescriptor.GetCustomAttributes<UpdateAttribute>().Count > 0
                || actionDescriptor.GetCustomAttributes<PartialUpdateAttribute>().Count > 0))
            {
                return _defaultODataParameterDeserializerValue.Value;
            }

            return base.GetODataDeserializer(type, request);
        }
    }
}
