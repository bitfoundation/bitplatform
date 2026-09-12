using Microsoft.AspNetCore.OData.Query.Wrapper;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// Keeps <c>$select</c> and <c>$expand</c> answers in the same casing as every other response.
/// <para>
/// OData projects a selected row into an <see cref="ISelectExpandWrapper"/>, and its own converter writes that as a
/// dictionary keyed by the EDM (CLR) property names. A dictionary key is not a property name, so
/// <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> never touches it: <c>?$select=Name</c> answers
/// <c>{"Name":...}</c> where the unselected row answers <c>{"name":...}</c> - two shapes for one endpoint, and neither
/// the OpenAPI schema nor a generated client expects the first.
/// </para>
/// </summary>
public class SelectExpandWrapperJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeof(ISelectExpandWrapper).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new Converter();

    private sealed class Converter : JsonConverter<ISelectExpandWrapper>
    {
        public override ISelectExpandWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException($"{nameof(ISelectExpandWrapper)} is a projection of a query, so nothing sends one back.");
        }

        public override void Write(Utf8JsonWriter writer, ISelectExpandWrapper value, JsonSerializerOptions options)
        {
            // The same mapper OData uses, so [JsonPropertyName] and [JsonIgnore] keep deciding what a property is called.
            var properties = value.ToDictionary(SelectExpandWrapperConverter.MapperProvider);

            writer.WriteStartObject();

            foreach (var property in properties)
            {
                writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(property.Key) ?? property.Key);
                JsonSerializer.Serialize(writer, property.Value, property.Value?.GetType() ?? typeof(object), options);
            }

            writer.WriteEndObject();
        }
    }
}
