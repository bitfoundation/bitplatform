using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.OData.Formatter.Deserialization;
using Bit.OData.ODataControllers;
using System.Web.Http.Controllers;

namespace Bit.OData.Serialization
{
    public class ExtendedODataDeserializerProvider : DefaultODataDeserializerProvider
    {
#if DEBUG
        protected ExtendedODataDeserializerProvider()
            : base(null)
        {
        }
#endif

        public ExtendedODataDeserializerProvider(IServiceProvider rootContainer)
            : base(rootContainer)
        {
            _DefaultODataParameterDeserializerValue = new Lazy<DefaultODataActionParameterDeserializer>(() => (DefaultODataActionParameterDeserializer)rootContainer.GetService(typeof(DefaultODataActionParameterDeserializer).GetTypeInfo()));
        }

        private readonly Lazy<DefaultODataActionParameterDeserializer> _DefaultODataParameterDeserializerValue;

        public override ODataDeserializer GetODataDeserializer(Type type, HttpRequestMessage request)
        {
            HttpActionDescriptor actionDescriptor = request.GetActionDescriptor();

            if (actionDescriptor != null && actionDescriptor.GetCustomAttributes<ActionAttribute>().Any())
            {
                return _DefaultODataParameterDeserializerValue.Value;
            }

            return base.GetODataDeserializer(type, request);
        }
    }
}
