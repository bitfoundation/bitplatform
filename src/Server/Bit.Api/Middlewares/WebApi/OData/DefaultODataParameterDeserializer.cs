using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.OData;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData;
using System.Linq;
using System.Collections;

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
                PropertyInfo prop = typeInfo.GetProperty(keyVal.Key);

                if (prop == null)
                    continue;

                object value = null;

                if (typeof(string) != prop.PropertyType && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(prop.PropertyType))
                {
                    TypeInfo elementType = prop.PropertyType.HasElementType ? prop.PropertyType.GetElementType().GetTypeInfo() : prop.PropertyType.GetGenericArguments().First().GetTypeInfo();
                    value = ToListMethod.MakeGenericMethod(elementType).Invoke(null, new[] { keyVal.Value });
                }
                else
                    value = keyVal.Value;

                prop.SetValue(result, value);
            }

            return result;
        }

        private MethodInfo ToListMethod = typeof(DefaultODataParameterDeserializer).GetMethod(nameof(ToList), BindingFlags.NonPublic | BindingFlags.Static);

        private static List<T> ToList<T>(IEnumerable<T> source)
        {
            return source.ToList();
        }
    }
}
