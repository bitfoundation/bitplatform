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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;

namespace Bit.OData.Serialization
{
    public class DefaultODataActionCreateUpdateParameterDeserializer : ODataDeserializer
    {
        private readonly ODataJsonDeSerializerStringCorrector _stringCorrectorsConverters = default!;
        private readonly ODataJsonDeSerializerEnumConverter _odataJsonDeserializerEnumConverter = default!;

        protected DefaultODataActionCreateUpdateParameterDeserializer()
            : base(ODataPayloadKind.Parameter)
        {

        }

        public DefaultODataActionCreateUpdateParameterDeserializer(IEnumerable<IStringCorrector> stringCorrectors)
            : base(ODataPayloadKind.Parameter)
        {
            _stringCorrectorsConverters = new ODataJsonDeSerializerStringCorrector(stringCorrectors.ToArray());
            _odataJsonDeserializerEnumConverter = new ODataJsonDeSerializerEnumConverter();
        }

        public override object Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)
        {
            if (readContext == null)
                throw new ArgumentNullException(nameof(readContext));

            HttpActionDescriptor actionDescriptor = readContext.Request.GetActionDescriptor();

            if (actionDescriptor != null && !actionDescriptor.GetCustomAttributes<ActionAttribute>().Any() && !actionDescriptor.GetCustomAttributes<CreateAttribute>().Any() && !actionDescriptor.GetCustomAttributes<UpdateAttribute>().Any() && !actionDescriptor.GetCustomAttributes<PartialUpdateAttribute>().Any())
                throw new InvalidOperationException($"{nameof(DefaultODataActionCreateUpdateParameterDeserializer)} is designed for odata actions|creates|updates|partialUpdates only");

            TypeInfo typeInfo = type.GetTypeInfo();

            IDependencyResolver dependencyResolver = readContext.Request.GetOwinContext()
                .GetDependencyResolver();

            ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

            JToken requestJsonBody = (JToken)readContext.Request.Properties["ContentStreamAsJson"];

            using (JsonReader requestJsonReader = requestJsonBody.CreateReader())
            {
                void Error(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
                {
                    if (e.ErrorContext.Error is JsonSerializationException && e.ErrorContext.Error.Message.StartsWith("Could not find member ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (e.CurrentObject is IOpenType openDto)
                        {
                            openDto.Properties = openDto.Properties ?? new Dictionary<string, object?>();
                            if (requestJsonReader.Read())
                                openDto.Properties.Add((string)(e.ErrorContext.Member ?? "UnknownMember"), requestJsonReader.Value);
                        }

                        e.ErrorContext.Handled = true;
                    }

                    if (e.ErrorContext.Handled == false)
                    {
                        readContext.Request.Properties["Request_Body_Json_Parse_Error"] = e.ErrorContext.Error; // This code is being executed in a try/catch which is located in ODataMediaTypeFormatter. That class will return default value (null) to actions which results into NRE in most cases.
                    }
                }

                JsonSerializerSettings settings = DefaultJsonContentFormatter.DeserializeSettings();

                settings.Converters = new JsonConverter[]
                {
                        _odataJsonDeserializerEnumConverter,
                        _stringCorrectorsConverters
                };

                settings.MissingMemberHandling = MissingMemberHandling.Error;

                JsonSerializer deserilizer = JsonSerializer.Create(settings);

                deserilizer.Error += Error;

                try
                {
                    object? result = null;

                    if (!typeof(Delta).GetTypeInfo().IsAssignableFrom(typeInfo))
                        result = deserilizer.Deserialize(requestJsonReader, typeInfo)!;
                    else
                    {
                        List<string> changedPropNames = new List<string>();

                        using (JsonReader jsonReaderForGettingSchema = requestJsonBody.CreateReader())
                        {
                            while (jsonReaderForGettingSchema.Read())
                            {
                                if (jsonReaderForGettingSchema.Value != null && jsonReaderForGettingSchema.TokenType == JsonToken.PropertyName)
                                {
                                    changedPropNames.Add(jsonReaderForGettingSchema.Value.ToString()!);
                                }
                            }
                        }

                        TypeInfo dtoType = typeInfo.GetGenericArguments().ExtendedSingle("Finding dto type from delta").GetTypeInfo();

                        object? modifiedDto = deserilizer.Deserialize(requestJsonReader, dtoType);

                        Delta delta = (Delta)(Activator.CreateInstance(typeInfo)!);

                        if (modifiedDto is IOpenType openTypeDto && openTypeDto.Properties?.Any() == true)
                            delta.TrySetPropertyValue(nameof(IOpenType.Properties), openTypeDto);

                        foreach (string changedProp in changedPropNames.Where(p => p != nameof(IOpenType.Properties) && dtoType.GetProperty(p) != null))
                        {
                            delta.TrySetPropertyValue(changedProp, (dtoType.GetProperty(changedProp) ?? throw new InvalidOperationException($"{changedProp} could not be found in {dtoType.FullName}")).GetValue(modifiedDto));
                        }

                        result = delta;
                    }

                    return result;
                }
                finally
                {
                    deserilizer.Error -= Error;
                }
            }
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

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
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

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
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

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            Type unwrappedType = objectType.IsEnum ? objectType : Nullable.GetUnderlyingType(objectType)!;

            if (unwrappedType == null)
                throw new InvalidOperationException($"{nameof(unwrappedType)} is null");

            string fullName = unwrappedType.FullName!;

            string enumRawValue = (string)reader.Value;

            enumRawValue = enumRawValue.Replace(fullName, string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace("'", string.Empty, StringComparison.InvariantCultureIgnoreCase);

            return Enum.Parse(unwrappedType, enumRawValue);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
