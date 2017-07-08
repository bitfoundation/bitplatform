using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.OData.Formatter.Deserialization;
using Bit.OData.ODataControllers;

namespace Bit.OData.Serialization
{
    public class ExtendedODataDeserializerProvider : DefaultODataDeserializerProvider
    {
        private readonly IServiceProvider _rootContainer;

        protected ExtendedODataDeserializerProvider()
            : base(null)
        {

        }

        public ExtendedODataDeserializerProvider(IServiceProvider rootContainer)
            : base(rootContainer)
        {
            _rootContainer = rootContainer;

            _DefaultODataParameterDeserializerValue = new Lazy<DefaultODataActionParameterDeserializer>(() =>
            {
                return (DefaultODataActionParameterDeserializer)_rootContainer.GetService(typeof(DefaultODataActionParameterDeserializer).GetTypeInfo());
            });
        }

        private readonly Lazy<DefaultODataActionParameterDeserializer> _DefaultODataParameterDeserializerValue;

        public override ODataDeserializer GetODataDeserializer(Type type, HttpRequestMessage request)
        {
            if (request.GetActionDescriptor().GetCustomAttributes<ActionAttribute>().Any())
            {
                return _DefaultODataParameterDeserializerValue.Value;
            }

            return base.GetODataDeserializer(type, request);
        }
    }
}
