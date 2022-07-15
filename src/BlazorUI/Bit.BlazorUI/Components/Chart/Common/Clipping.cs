using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Represents how lines are clipped relative to the chart area.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#line-styling">here (Chart.js)</a>.
/// <para>For any given edge:
/// <list type="bullet">
/// <item>0 means clipping at the chart area.</item>
/// <item>negative values mean clipping inside the chart area.</item>
/// <item>positive values mean clipping outside the chart area.</item>
/// <item><see langword="null"/> means no clipping.</item>
/// </list>
/// </para>
/// </summary>
[JsonConverter(typeof(ClippingJsonConverter))]
public readonly struct Clipping : IEquatable<Clipping>
{
    internal readonly bool _equalSides;

    /// <summary>
    /// Gets the clipping for the top edge.
    /// </summary>
    public int? Top { get; }

    /// <summary>
    /// Gets the clipping for the right edge.
    /// </summary>
    public int? Right { get; }

    /// <summary>
    /// Gets the clipping for the bottom edge.
    /// </summary>
    public int? Bottom { get; }

    /// <summary>
    /// Gets the clipping for the left edge.
    /// </summary>
    public int? Left { get; }

    /// <summary>
    /// Creates a new instance of <see cref="Clipping"/>
    /// using the supplied value for all edges.
    /// </summary>
    /// <param name="all">The clipping value for all edges.</param>
    public Clipping(int all)
    {
        Top = Right = Bottom = Left = all;
        _equalSides = true;
    }

    /// <summary>
    /// Creates a new instance of <see cref="Clipping"/>
    /// using individual values for all edges.
    /// </summary>
    /// <param name="bottom">The clipping value for the bottom edge.</param>
    /// <param name="left">The clipping value for the left edge.</param>
    /// <param name="top">The clipping value for the top edge.</param>
    /// <param name="right">The clipping value for the right edge.</param>
    public Clipping(int? top = null, int? right = null, int? bottom = null, int? left = null)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
        _equalSides = false;
    }

    /// <summary>
    /// Converts an <see cref="int"/> value to a <see cref="Clipping"/> implicitly.
    /// The supplied value will be used for all edges.
    /// </summary>
    /// <param name="value">The clipping value for all edges.</param>
    public static implicit operator Clipping(int value) => new Clipping(value);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(Top)}: {ValOrNull(Top)}, " +
               $"{nameof(Right)}: {ValOrNull(Right)}, " +
               $"{nameof(Bottom)}: {ValOrNull(Bottom)}, " +
               $"{nameof(Left)}: {ValOrNull(Left)}";

        static string ValOrNull(int? value) => value.HasValue ? value.Value.ToString() : "null";
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool Equals(object obj) => obj is Clipping clipping && Equals(clipping);
    public bool Equals(Clipping other)
    {
        if (_equalSides && other._equalSides)
            return Top.Value == other.Top.Value;

        return Top == other.Top &&
               Right == other.Right &&
               Bottom == other.Bottom &&
               Left == other.Left;
    }

    public override int GetHashCode() => HashCode.Combine(Bottom, Left, Top, Right);

    public static bool operator ==(Clipping left, Clipping right) => left.Equals(right);
    public static bool operator !=(Clipping left, Clipping right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

internal class ClippingJsonConverter : JsonConverter<Clipping>
{
    public override Clipping ReadJson(JsonReader reader, Type objectType, [AllowNull] Clipping existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
            return new Clipping((int)(long)reader.Value);

        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonReaderException();

        JObject obj = JObject.Load(reader);
        int? top = GetClippingValue(obj, nameof(Clipping.Top));
        int? right = GetClippingValue(obj, nameof(Clipping.Right));
        int? bottom = GetClippingValue(obj, nameof(Clipping.Bottom));
        int? left = GetClippingValue(obj, nameof(Clipping.Left));

        return new Clipping(top, right, bottom, left);
    }

    private static int? GetClippingValue(JObject obj, string name)
    {
        if (!obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken token))
            return null;

        if (token.Type == JTokenType.Boolean && (bool)token == false)
            return null;

        if (token.Type == JTokenType.Integer)
            return (int)token;

        throw new JsonWriterException();
    }

    public override void WriteJson(JsonWriter writer, [AllowNull] Clipping value, JsonSerializer serializer)
    {
        if (value._equalSides)
        {
            writer.WriteValue(value.Bottom.Value);
            return;
        }

        NamingStrategy naming = (serializer.ContractResolver as DefaultContractResolver)?.NamingStrategy;

        writer.WriteStartObject();

        WriteAdjustedName(writer, naming, nameof(Clipping.Top));
        WriteValueOrFalse(writer, value.Top);

        WriteAdjustedName(writer, naming, nameof(Clipping.Right));
        WriteValueOrFalse(writer, value.Right);

        WriteAdjustedName(writer, naming, nameof(Clipping.Bottom));
        WriteValueOrFalse(writer, value.Bottom);

        WriteAdjustedName(writer, naming, nameof(Clipping.Left));
        WriteValueOrFalse(writer, value.Left);

        writer.WriteEndObject();
    }

    private static void WriteAdjustedName(JsonWriter writer, NamingStrategy namingStrategy, string name)
    {
        if (namingStrategy != null)
        {
            name = namingStrategy.GetPropertyName(name, false);
        }

        writer.WritePropertyName(name);
    }

    private static void WriteValueOrFalse(JsonWriter writer, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteValue(value.Value);
        }
        else
        {
            writer.WriteValue(false);
        }
    }
}
