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
            _defaultODataParameterDeserializerValue = new Lazy<DefaultODataActionCreateUpdateParameterDeserializer>(() => (DefaultODataActionCreateUpdateParameterDeserializer)rootContainer.GetService(typeof(DefaultODataActionCreateUpdateParameterDeserializer).GetTypeInfo()), isThreadSafe: true);
        }

        private readonly Lazy<DefaultODataActionCreateUpdateParameterDeserializer> _defaultODataParameterDeserializerValue;

        public override ODataDeserializer GetODataDeserializer(Type type, HttpRequestMessage request)
        {
            HttpActionDescriptor actionDescriptor = request.GetActionDescriptor();

            if (actionDescriptor != null && (actionDescriptor.GetCustomAttributes<ActionAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<CreateAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<UpdateAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<PartialUpdateAttribute>().Any()))
            {
                return _defaultODataParameterDeserializerValue.Value;
            }

            return base.GetODataDeserializer(type, request);
        }
    }
}
