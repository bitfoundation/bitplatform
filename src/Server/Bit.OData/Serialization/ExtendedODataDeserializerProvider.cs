using Bit.OData.ODataControllers;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.OData.Formatter.Deserialization;

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
            _DefaultODataParameterDeserializerValue = new Lazy<DefaultODataActionCreateUpdateParameterDeserializer>(() => (DefaultODataActionCreateUpdateParameterDeserializer)rootContainer.GetService(typeof(DefaultODataActionCreateUpdateParameterDeserializer).GetTypeInfo()));
        }

        private readonly Lazy<DefaultODataActionCreateUpdateParameterDeserializer> _DefaultODataParameterDeserializerValue;

        public override ODataDeserializer GetODataDeserializer(Type type, HttpRequestMessage request)
        {
            HttpActionDescriptor actionDescriptor = request.GetActionDescriptor();

            if (actionDescriptor != null && (actionDescriptor.GetCustomAttributes<ActionAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<CreateAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<UpdateAttribute>().Any()))
            {
                return _DefaultODataParameterDeserializerValue.Value;
            }

            return base.GetODataDeserializer(type, request);
        }
    }
}
