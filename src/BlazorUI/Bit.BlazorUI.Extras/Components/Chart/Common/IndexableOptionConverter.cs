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

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(value);

        // get the correct property using reflection
        var prop = value.GetType().GetProperty(BitChartIndexableOption<object>.PropertyName, BindingFlags.Instance | BindingFlags.NonPublic);


        try
        {
            // if the indexable option was not initialized, this will throw an InvalidOperationException wrapped in a TargetInvocationException
            var toWrite = prop?.GetValue(value);

            if (toWrite is null) return;

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
