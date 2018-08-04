using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Contracts;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.OData;
using Microsoft.Owin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;

namespace Bit.OData.Serialization
{
    public class DefaultODataActionCreateUpdateParameterDeserializer : ODataDeserializer
    {
        private readonly ODataJsonDeSerializerStringCorrector _stringCorrectorsConverters;
        private readonly ODataJsonDeSerializerEnumConverter _odataJsonDeserializerEnumConverter;

        protected DefaultODataActionCreateUpdateParameterDeserializer()
            : base(ODataPayloadKind.Parameter)
        {

        }

        public DefaultODataActionCreateUpdateParameterDeserializer(System.Web.Http.Dependencies.IDependencyResolver dependencyResolver)
            : base(ODataPayloadKind.Parameter)
        {
            if (dependencyResolver == null)
                throw new ArgumentNullException(nameof(dependencyResolver));

            _stringCorrectorsConverters = new ODataJsonDeSerializerStringCorrector(dependencyResolver.GetServices(typeof(IStringCorrector).GetTypeInfo()).Cast<IStringCorrector>().ToArray());
            _odataJsonDeserializerEnumConverter = new ODataJsonDeSerializerEnumConverter();
        }

        public override object Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)
        {
            HttpActionDescriptor actionDescriptor = readContext.Request.GetActionDescriptor();

            if (actionDescriptor?.GetCustomAttributes<ActionAttribute>().Count == 0
                && actionDescriptor.GetCustomAttributes<CreateAttribute>().Count == 0
                && actionDescriptor.GetCustomAttributes<UpdateAttribute>().Count == 0
                && actionDescriptor.GetCustomAttributes<PartialUpdateAttribute>().Count == 0)
            {
                throw new InvalidOperationException($"{nameof(DefaultODataActionCreateUpdateParameterDeserializer)} is designed for odata actions|creates|updates|partialUpdates only");
            }

            TypeInfo typeInfo = type.GetTypeInfo();

            IDependencyResolver dependencyResolver = readContext.Request.GetOwinContext()
                .GetDependencyResolver();

            ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

            string requestJsonBody = (string)readContext.Request.Properties["ContentStreamAsJson"];

            using (StringReader jsonStringReader = new StringReader(requestJsonBody))
            using (JsonTextReader requestJsonReader = new JsonTextReader(jsonStringReader))
            {
                void Error(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
                {
                    if (e.ErrorContext.Error is JsonSerializationException && e.ErrorContext.Error.Message.StartsWith("Could not find member ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (e.CurrentObject is IOpenType openDto)
                        {
                            openDto.Properties = openDto.Properties ?? new Dictionary<string, object>();
                            if (requestJsonReader.Read())
                                openDto.Properties.Add((string)e.ErrorContext.Member, requestJsonReader.Value);
                        }

                        e.ErrorContext.Handled = true;
                    }
                }

                JsonSerializerSettings settings = DefaultJsonContentFormatter.DeSerializeSettings();

                settings.Converters = new JsonConverter[]
                {
                        _odataJsonDeserializerEnumConverter,
                        _stringCorrectorsConverters,
                        new ODataJsonDeSerializerDateTimeOffsetTimeZone(timeZoneManager)
                };

                settings.MissingMemberHandling = MissingMemberHandling.Error;

                JsonSerializer deserilizer = JsonSerializer.Create(settings);

                deserilizer.Error += Error;

                try
                {
                    if (!typeof(Delta).GetTypeInfo().IsAssignableFrom(typeInfo))
                        return deserilizer.Deserialize(requestJsonReader, typeInfo);
                    List<string> changedPropNames = new List<string>();

                    using (JsonTextReader jsonReaderForGettingSchema = new JsonTextReader(new StringReader(requestJsonBody)))
                    {
                        while (jsonReaderForGettingSchema.Read())
                        {
                            if (jsonReaderForGettingSchema.Value != null && jsonReaderForGettingSchema.TokenType == JsonToken.PropertyName)
                                changedPropNames.Add(jsonReaderForGettingSchema.Value.ToString());
                        }
                    }

                    TypeInfo dtoType = typeInfo.GetGenericArguments().ExtendedSingle("Finding dto type from delta").GetTypeInfo();

                    object modifiedDto = deserilizer.Deserialize(requestJsonReader, dtoType);

                    Delta delta = (Delta)Activator.CreateInstance(typeInfo);

                    if (modifiedDto is IOpenType openTypeDto && openTypeDto.Properties?.Any() == true)
                        delta.TrySetPropertyValue(nameof(IOpenType.Properties), openTypeDto);

                    foreach (string changedProp in changedPropNames.Where(p => p != nameof(IOpenType.Properties) && dtoType.GetProperty(p) != null))
                    {
                        delta.TrySetPropertyValue(changedProp, dtoType.GetProperty(changedProp).GetValue(modifiedDto));
                    }

                    return delta;
                }
                finally
                {
                    deserilizer.Error -= Error;
                }
            }
        }
    }

    internal class ODataJsonDeSerializerDateTimeOffsetTimeZone : JsonConverter
    {
        private readonly ITimeZoneManager _timeZoneManager;

        public ODataJsonDeSerializerDateTimeOffsetTimeZone(ITimeZoneManager timeZoneManager)
        {
            _timeZoneManager = timeZoneManager;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || Nullable.GetUnderlyingType(objectType) == typeof(DateTimeOffset);
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            DateTimeOffset objAsDateTimeOffset = (DateTime)reader.Value;

            return _timeZoneManager.MapFromClientToServer(objAsDateTimeOffset);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    internal class ODataJsonDeSerializerStringCorrector : JsonConverter
    {
        private readonly IEnumerable<IStringCorrector> _stringCorrectors;

        public ODataJsonDeSerializerStringCorrector(IEnumerable<IStringCorrector> stringCorrectors)
        {
            _stringCorrectors = stringCorrectors;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            string rawString = (string)reader.Value;

            foreach (IStringCorrector stringCorrector in _stringCorrectors)
            {
                rawString = stringCorrector.CorrectString(rawString);
            }

            return rawString;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    internal class ODataJsonDeSerializerEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum || Nullable.GetUnderlyingType(objectType)?.IsEnum == true;
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            Type unwrappedType = objectType.IsEnum ? objectType : Nullable.GetUnderlyingType(objectType);

            if (unwrappedType == null)
                throw new InvalidOperationException($"{nameof(unwrappedType)} is null");

            string fullName = unwrappedType.FullName;

            string enumRawValue = (string)reader.Value;

            enumRawValue = enumRawValue.Replace(fullName, string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace("'", string.Empty, StringComparison.InvariantCultureIgnoreCase);

            return Enum.Parse(unwrappedType, enumRawValue);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
