using Bit.OData.ODataControllers;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;

namespace Bit.OData.Serialization
{
    public class ExtendedODataDeserializerProvider : DefaultODataDeserializerProvider
    {
        protected ExtendedODataDeserializerProvider()
            : base(null)
        {
        }

        public ExtendedODataDeserializerProvider(IServiceProvider rootContainer, DefaultODataActionCreateUpdateParameterDeserializer defaultODataActionCreateUpdateParameterDeserializer)
            : base(rootContainer)
        {
            _defaultODataActionCreateUpdateParameterDeserializer = defaultODataActionCreateUpdateParameterDeserializer;
        }

        readonly DefaultODataActionCreateUpdateParameterDeserializer _defaultODataActionCreateUpdateParameterDeserializer = default!;

        public override ODataDeserializer GetODataDeserializer(Type type, HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            HttpActionDescriptor actionDescriptor = request.GetActionDescriptor();

            if (actionDescriptor != null && request?.Content?.Headers?.ContentLength != 0 && (actionDescriptor.GetCustomAttributes<ActionAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<CreateAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<UpdateAttribute>().Any() ||
                actionDescriptor.GetCustomAttributes<PartialUpdateAttribute>().Any()))
            {
                return _defaultODataActionCreateUpdateParameterDeserializer;
            }

            return base.GetODataDeserializer(type, request);
        }
    }
}
