using Foundation.Api.ApiControllers;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.OData.Formatter.Deserialization;

namespace Foundation.Api.Middlewares.WebApi.OData
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

            _DefaultODataParameterDeserializerValue = new Lazy<DefaultODataParameterDeserializer>(() =>
            {
                return (DefaultODataParameterDeserializer)_rootContainer.GetService(typeof(DefaultODataParameterDeserializer).GetTypeInfo());
            });
        }

        private readonly Lazy<DefaultODataParameterDeserializer> _DefaultODataParameterDeserializerValue;

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
