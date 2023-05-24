using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI;

internal class IndexableOptionConverter : JsonConverter
{
    public override bool CanRead => false;
    public override bool CanWrite => true;

    public override bool CanConvert(Type objectType)
    {
        if (!objectType.IsGenericType) return false;

        return objectType.GetGenericTypeDefinition() == typeof(BitChartIndexableOption<>);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // get the correct property using reflection
        var prop = value.GetType().GetProperty(BitChartIndexableOption<object>.PropertyName, BindingFlags.Instance | BindingFlags.NonPublic);

        try
        {
            // if the indexable option was not initialized, this will throw an InvalidOperationException wrapped in a TargetInvocationException
            object toWrite = prop.GetValue(value);

            // write the property
            JToken.FromObject(toWrite).WriteTo(writer);
        }
        catch (TargetInvocationException oex)
            when (oex.InnerException is InvalidOperationException iex)
        {
            Console.WriteLine("Error while trying to serialize an indexable option:");
            Console.WriteLine(iex.Message);

            // write undefined value, chart.js will use their default
            writer.WriteUndefined();
        }
    }
}
