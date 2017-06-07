using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.OData;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData;

namespace Bit.Api.Middlewares.WebApi.OData
{
    public class DefaultODataParameterDeserializer : ODataDeserializer
    {
        private readonly ODataActionPayloadDeserializer _actionPayloadDeserializer;

        protected DefaultODataParameterDeserializer()
            : base(ODataPayloadKind.Parameter)
        {

        }

        public DefaultODataParameterDeserializer(ODataDeserializerProvider deserializerProvider, ODataActionPayloadDeserializer actionPayloadDeserializer)
            : base(ODataPayloadKind.Parameter)
        {
            _actionPayloadDeserializer = actionPayloadDeserializer;
        }

        public override object Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)
        {
            TypeInfo typeInfo = type.GetTypeInfo();

            ODataActionParameters payload = (ODataActionParameters)_actionPayloadDeserializer.Read(messageReader, typeof(ODataActionParameters).GetTypeInfo(), readContext);

            object result = Activator.CreateInstance(typeInfo);

            foreach (KeyValuePair<string, object> keyVal in payload)
            {
                typeInfo.GetProperty(keyVal.Key)?.SetValue(result, keyVal.Value);
            }

            return result;
        }
    }
}
