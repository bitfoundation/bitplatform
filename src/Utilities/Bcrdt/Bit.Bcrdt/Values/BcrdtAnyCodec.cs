using System.Globalization;
using System.Text.Json;

namespace Bit.Bcrdt;

/// <summary>Wire tags for the collaborative value model.</summary>
/// <remarks>
/// These numbers are part of the persisted format. Existing tags must never be renumbered or
/// reused; new value kinds take the next free number so that an old reader fails cleanly on a new
/// document rather than misinterpreting it.
/// </remarks>
public enum BcrdtAnyTag : byte
{
    /// <summary>No value at all - distinct from <see cref="Null"/>, used for "key absent".</summary>
    Undefined = 0,
    /// <summary>An explicit null.</summary>
    Null = 1,
    /// <summary>A 64-bit signed integer, zigzag varint encoded.</summary>
    Int = 2,
    /// <summary>An IEEE-754 double.</summary>
    Float64 = 3,
    /// <summary>An IEEE-754 single.</summary>
    Float32 = 4,
    /// <summary>Boolean true. The value is carried by the tag itself.</summary>
    True = 5,
    /// <summary>Boolean false.</summary>
    False = 6,
    /// <summary>A UTF-8 string.</summary>
    String = 7,
    /// <summary>An opaque byte array.</summary>
    Binary = 8,
    /// <summary>An ordered list of values.</summary>
    Array = 9,
    /// <summary>A string-keyed map of values, always written with sorted keys.</summary>
    Object = 10,
    /// <summary>A <see cref="DateTimeOffset"/> as UTC ticks plus an offset in minutes.</summary>
    Timestamp = 11,
    /// <summary>A <see cref="Guid"/> as sixteen raw bytes.</summary>
    Guid = 12,
    /// <summary>A <see cref="decimal"/> as its four constituent 32-bit words.</summary>
    Decimal = 13,
}

/// <summary>
/// Encodes and decodes the value model that collaborative maps, arrays and embeds store.
/// </summary>
/// <remarks>
/// <para>
/// Values are represented as plain <see cref="object"/> graphs built from a closed set of types:
/// <see langword="null"/>, <see cref="bool"/>, <see cref="long"/>, <see cref="double"/>,
/// <see cref="float"/>, <see cref="string"/>, <see cref="byte"/> arrays, <see cref="DateTimeOffset"/>,
/// <see cref="Guid"/>, <see cref="decimal"/>, lists and string-keyed dictionaries. Arbitrary
/// application types are projected onto that set by <see cref="FromObject"/> and rehydrated by
/// <see cref="ToObject{T}"/>.
/// </para>
/// <para>
/// <b>Object keys are always written in ordinal sort order.</b> Two replicas that store logically
/// equal objects therefore produce byte-identical encodings, which is what allows encoded values to
/// be used as set membership keys and allows updates to be compared for equality in tests.
/// </para>
/// </remarks>
public static class BcrdtAnyCodec
{
    private const int MaxDepth = 64;

    /// <summary>Writes a value, including its tag.</summary>
    /// <exception cref="NotSupportedException">
    /// The value's runtime type is outside the supported set. Call <see cref="FromObject"/> first to
    /// project an arbitrary type onto the value model.
    /// </exception>
    public static void Write(ref BcrdtBufferWriter writer, object? value) => Write(ref writer, value, 0);

    private static void Write(ref BcrdtBufferWriter writer, object? value, int depth)
    {
        if (depth > MaxDepth) throw new NotSupportedException($"Value nests deeper than {MaxDepth} levels.");

        switch (value)
        {
            case null:
                writer.WriteByte((byte)BcrdtAnyTag.Null);
                return;
            case bool b:
                writer.WriteByte((byte)(b ? BcrdtAnyTag.True : BcrdtAnyTag.False));
                return;
            case string s:
                writer.WriteByte((byte)BcrdtAnyTag.String);
                writer.WriteString(s);
                return;
            case long l:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt(l);
                return;
            case int i:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt(i);
                return;
            case short sh:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt(sh);
                return;
            case ushort us:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt(us);
                return;
            case byte by:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt(by);
                return;
            case sbyte sb:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt(sb);
                return;
            case uint ui:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt(ui);
                return;
            case ulong ul when ul <= long.MaxValue:
                writer.WriteByte((byte)BcrdtAnyTag.Int);
                writer.WriteVarInt((long)ul);
                return;
            case double d:
                writer.WriteByte((byte)BcrdtAnyTag.Float64);
                writer.WriteDouble(d);
                return;
            case float f:
                writer.WriteByte((byte)BcrdtAnyTag.Float32);
                writer.WriteSingle(f);
                return;
            case decimal m:
                writer.WriteByte((byte)BcrdtAnyTag.Decimal);
                foreach (int part in decimal.GetBits(m)) writer.WriteVarInt(part);
                return;
            case byte[] bytes:
                writer.WriteByte((byte)BcrdtAnyTag.Binary);
                writer.WriteBytes(bytes);
                return;
            case DateTimeOffset dto:
                writer.WriteByte((byte)BcrdtAnyTag.Timestamp);
                writer.WriteVarInt(dto.UtcTicks);
                writer.WriteVarInt((long)dto.Offset.TotalMinutes);
                return;
            case DateTime dt:
                writer.WriteByte((byte)BcrdtAnyTag.Timestamp);
                writer.WriteVarInt(dt.ToUniversalTime().Ticks);
                writer.WriteVarInt(0);
                return;
            case Guid guid:
                writer.WriteByte((byte)BcrdtAnyTag.Guid);
                Span<byte> guidBytes = stackalloc byte[16];
                guid.TryWriteBytes(guidBytes);
                writer.WriteRaw(guidBytes);
                return;
            case IReadOnlyDictionary<string, object?> map:
                WriteObject(ref writer, map, depth);
                return;
            case IDictionary<string, object?> mutableMap:
                WriteObject(ref writer, mutableMap.ToDictionary(), depth);
                return;
            case System.Collections.IEnumerable list and not string:
                WriteArray(ref writer, list, depth);
                return;
            default:
                throw new NotSupportedException(
                    $"Type '{value.GetType()}' is not part of the collaborative value model. " +
                    "Use BcrdtAnyCodec.FromObject to project it first.");
        }
    }

    private static void WriteObject(ref BcrdtBufferWriter writer, IReadOnlyDictionary<string, object?> map, int depth)
    {
        writer.WriteByte((byte)BcrdtAnyTag.Object);
        writer.WriteVarUInt((ulong)map.Count);

        // Sorted keys make the encoding canonical: logically equal objects encode identically on
        // every replica, which set membership and update-equality assertions depend on.
        string[] keys = [.. map.Keys];
        Array.Sort(keys, StringComparer.Ordinal);
        foreach (string key in keys)
        {
            writer.WriteString(key);
            Write(ref writer, map[key], depth + 1);
        }
    }

    private static void WriteArray(ref BcrdtBufferWriter writer, System.Collections.IEnumerable list, int depth)
    {
        // Materialize first: the count prefix has to be written before the elements, and a general
        // IEnumerable cannot be counted without enumerating it.
        List<object?> items = [];
        foreach (object? item in list) items.Add(item);

        writer.WriteByte((byte)BcrdtAnyTag.Array);
        writer.WriteVarUInt((ulong)items.Count);
        foreach (object? item in items) Write(ref writer, item, depth + 1);
    }

    /// <summary>Reads a value written by <see cref="Write(ref BcrdtBufferWriter, object?)"/>.</summary>
    public static object? Read(ref BcrdtBufferReader reader) => Read(ref reader, 0);

    private static object? Read(ref BcrdtBufferReader reader, int depth)
    {
        if (depth > MaxDepth) throw new BcrdtFormatException($"Value nests deeper than {MaxDepth} levels.");

        byte tag = reader.ReadByte();
        switch ((BcrdtAnyTag)tag)
        {
            case BcrdtAnyTag.Undefined:
            case BcrdtAnyTag.Null:
                return null;
            case BcrdtAnyTag.True:
                return true;
            case BcrdtAnyTag.False:
                return false;
            case BcrdtAnyTag.Int:
                return reader.ReadVarInt();
            case BcrdtAnyTag.Float64:
                return reader.ReadDouble();
            case BcrdtAnyTag.Float32:
                return reader.ReadSingle();
            case BcrdtAnyTag.String:
                return reader.ReadString();
            case BcrdtAnyTag.Binary:
                return reader.ReadBytes();
            case BcrdtAnyTag.Timestamp:
            {
                long ticks = reader.ReadVarInt();
                long offsetMinutes = reader.ReadVarInt();
                if (ticks < 0 || ticks > DateTime.MaxValue.Ticks || offsetMinutes is < -840 or > 840)
                {
                    throw new BcrdtFormatException("Timestamp is out of range.");
                }
                var offset = TimeSpan.FromMinutes(offsetMinutes);
                try
                {
                    return new DateTimeOffset(ticks + offset.Ticks, offset);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Each half is in range on its own, but together they name a local time that is not.
                    throw new BcrdtFormatException("Timestamp is out of range.", ex);
                }
            }
            case BcrdtAnyTag.Guid:
                return new Guid(reader.ReadRaw(16).Span);
            case BcrdtAnyTag.Decimal:
            {
                Span<int> parts = stackalloc int[4];
                for (int i = 0; i < 4; i++)
                {
                    long part = reader.ReadVarInt();
                    if (part is < int.MinValue or > int.MaxValue)
                    {
                        throw new BcrdtFormatException("Decimal word is out of range.");
                    }
                    parts[i] = (int)part;
                }
                try { return new decimal(parts); }
                catch (ArgumentException ex) { throw new BcrdtFormatException("Malformed decimal.", ex); }
            }
            case BcrdtAnyTag.Array:
            {
                int count = reader.ValidateCount(reader.ReadVarUInt32(), "array element");
                var items = new List<object?>(BcrdtBufferReader.InitialCapacity(count));
                for (int i = 0; i < count; i++) items.Add(Read(ref reader, depth + 1));
                return items;
            }
            case BcrdtAnyTag.Object:
            {
                int count = reader.ValidateCount(reader.ReadVarUInt32(), "object entry");
                var map = new Dictionary<string, object?>(BcrdtBufferReader.InitialCapacity(count), StringComparer.Ordinal);
                for (int i = 0; i < count; i++)
                {
                    string key = reader.ReadString();
                    map[key] = Read(ref reader, depth + 1);
                }
                return map;
            }
            default:
                throw BcrdtFormatException.UnknownTag("value", tag);
        }
    }

    /// <summary>Encodes a value to a standalone byte array.</summary>
    public static byte[] Encode(object? value)
    {
        var writer = new BcrdtBufferWriter(32);
        try
        {
            Write(ref writer, value);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>Decodes a value produced by <see cref="Encode"/>.</summary>
    public static object? Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new BcrdtBufferReader(data);
        return Read(ref reader);
    }

    /// <summary>
    /// Produces a stable string key for a value, suitable for set membership.
    /// </summary>
    /// <remarks>
    /// Two logically equal values always produce the same key on every replica, because
    /// <see cref="Write(ref BcrdtBufferWriter, object?)"/> is canonical. Short primitives get a readable key so that a document's
    /// internal state stays legible when inspected; everything else falls back to base64.
    /// </remarks>
    public static string CanonicalKey(object? value) => value switch
    {
        null => "\u0000null",
        string s => "s" + s,
        bool b => b ? "\u0000true" : "\u0000false",
        long or int or short or ushort or byte or sbyte or uint => "i" + Convert.ToInt64(value, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture),
        ulong ul when ul <= long.MaxValue => "i" + ul.ToString(CultureInfo.InvariantCulture),
        _ => "b" + Convert.ToBase64String(Encode(value)),
    };

    /// <summary>Structural equality over the value model, including nested arrays and objects.</summary>
    public static bool DeepEquals(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        if (left is byte[] leftBytes && right is byte[] rightBytes)
        {
            return leftBytes.AsSpan().SequenceEqual(rightBytes);
        }

        if (left is IReadOnlyDictionary<string, object?> leftMap &&
            right is IReadOnlyDictionary<string, object?> rightMap)
        {
            if (leftMap.Count != rightMap.Count) return false;
            foreach ((string key, object? value) in leftMap)
            {
                if (!rightMap.TryGetValue(key, out object? other) || !DeepEquals(value, other)) return false;
            }
            return true;
        }

        if (left is System.Collections.IEnumerable leftList and not string &&
            right is System.Collections.IEnumerable rightList and not string)
        {
            System.Collections.IEnumerator a = leftList.GetEnumerator();
            System.Collections.IEnumerator b = rightList.GetEnumerator();
            while (true)
            {
                bool hasA = a.MoveNext();
                bool hasB = b.MoveNext();
                if (hasA != hasB) return false;
                if (!hasA) return true;
                if (!DeepEquals(a.Current, b.Current)) return false;
            }
        }

        // Numeric widening: a value read back from the wire is a long or double even if it was
        // written as an int or a float, so comparing by runtime type alone would report false
        // inequality after a round trip.
        if (IsNumeric(left) && IsNumeric(right))
        {
            // Integers and decimals compare exactly. Routing them through double would make distinct
            // values beyond 2^53 - or decimals differing in their sixteenth digit - compare equal.
            if (left is double or float || right is double or float)
            {
                return Convert.ToDouble(left, CultureInfo.InvariantCulture)
                    .Equals(Convert.ToDouble(right, CultureInfo.InvariantCulture));
            }

            return Convert.ToDecimal(left, CultureInfo.InvariantCulture) ==
                Convert.ToDecimal(right, CultureInfo.InvariantCulture);
        }

        return left.Equals(right);
    }

    private static bool IsNumeric(object value) =>
        value is long or int or short or ushort or byte or sbyte or uint or ulong or double or float or decimal;

    /// <summary>
    /// Projects an arbitrary object onto the value model by way of JSON.
    /// </summary>
    /// <remarks>
    /// Application types do not need to be part of the value model to be stored collaboratively;
    /// they are flattened to nested dictionaries and lists here and rehydrated by
    /// <see cref="ToObject{T}"/>. The cost is one serialization round trip, so prefer storing
    /// primitives directly on hot paths.
    /// <para>
    /// The projection uses reflection-based System.Text.Json unless <paramref name="options"/> says
    /// otherwise. A trimmed or native AOT app passes options whose
    /// <see cref="JsonSerializerOptions.TypeInfoResolver"/> is a source-generated
    /// <c>JsonSerializerContext</c> covering the types it stores.
    /// </para>
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The options argument is the trimming seam: a trimmed or native AOT app passes options whose TypeInfoResolver is a source-generated JsonSerializerContext covering its types. Values of the collaborative value model never reach the serializer.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The options argument is the trimming seam: a trimmed or native AOT app passes options whose TypeInfoResolver is a source-generated JsonSerializerContext covering its types. Values of the collaborative value model never reach the serializer.")]
    public static object? FromObject(object? value, JsonSerializerOptions? options = null)
    {
        switch (value)
        {
            case null:
                return null;
            case string or bool or long or int or short or ushort or byte or sbyte or uint or double or float
                or decimal or byte[] or DateTime or DateTimeOffset or Guid:
                return Normalize(value);
            case ulong ul:
                // Beyond Int64 a decimal still holds the value exactly, where JSON would round it
                // through a double.
                return ul <= long.MaxValue ? (long)ul : (decimal)ul;
            default:
                return FromJson(JsonSerializer.SerializeToElement(value, options));
        }
    }

    /// <summary>Rehydrates a value-model graph into an application type.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The options argument is the trimming seam: a trimmed or native AOT app passes options whose TypeInfoResolver is a source-generated JsonSerializerContext covering its types. Values of the collaborative value model never reach the serializer.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The options argument is the trimming seam: a trimmed or native AOT app passes options whose TypeInfoResolver is a source-generated JsonSerializerContext covering its types. Values of the collaborative value model never reach the serializer.")]
    public static T? ToObject<T>(object? value, JsonSerializerOptions? options = null)
    {
        if (value is null) return default;
        if (value is T typed) return typed;
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, options), options);
    }

    /// <summary>Converts primitive aliases to their canonical value-model representation.</summary>
    public static object? Normalize(object? value) => value switch
    {
        null => null,
        int i => (long)i,
        short s => (long)s,
        ushort us => (long)us,
        byte b => (long)b,
        sbyte sb => (long)sb,
        uint u => (long)u,
        ulong ul when ul <= long.MaxValue => (long)ul,
        DateTime dt => new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero),
        _ => value,
    };

    /// <summary>
    /// Converts a value to exactly the representation a peer will decode it as, validating it on
    /// the way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Collaborative types store this result rather than the caller's object, for two reasons.
    /// First, it fails <i>before</i> anything is integrated: a value that cannot be encoded has to be
    /// rejected while rejecting it is still free, not after it has become part of the document and
    /// every later attempt to encode the document would throw. Second, lists and maps are copied
    /// rather than referenced, and primitives are widened to their wire types, so this replica holds
    /// exactly what every other replica decodes. Mutating a stored collection would otherwise change
    /// this replica's view without producing an update, and the replicas would silently diverge.
    /// </para>
    /// <para>
    /// Byte arrays are treated as immutable, as everywhere else in the value model, and are not
    /// copied.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// The value, or something nested inside it, is outside the collaborative value model.
    /// </exception>
    public static object? Canonicalize(object? value) => Canonicalize(value, 0);

    private static object? Canonicalize(object? value, int depth)
    {
        if (depth > MaxDepth) throw new NotSupportedException($"Value nests deeper than {MaxDepth} levels.");

        switch (value)
        {
            case null or bool or string or long or double or float or decimal or byte[] or DateTimeOffset or Guid:
                return value;
            case int or short or ushort or byte or sbyte or uint or DateTime:
                return Normalize(value);
            case ulong ul when ul <= long.MaxValue:
                return (long)ul;
            case IReadOnlyDictionary<string, object?> map:
                return CanonicalizeObject(map, depth);
            case IDictionary<string, object?> mutableMap:
                return CanonicalizeObject(mutableMap.ToDictionary(), depth);
            case System.Collections.IEnumerable list:
            {
                List<object?> items = [];
                foreach (object? item in list) items.Add(Canonicalize(item, depth + 1));
                return items;
            }
            default:
                throw new NotSupportedException(
                    $"Type '{value.GetType()}' is not part of the collaborative value model. " +
                    "Use BcrdtAnyCodec.FromObject to project it first.");
        }
    }

    private static Dictionary<string, object?> CanonicalizeObject(IReadOnlyDictionary<string, object?> map, int depth)
    {
        var copy = new Dictionary<string, object?>(map.Count, StringComparer.Ordinal);
        foreach ((string key, object? item) in map) copy[key] = Canonicalize(item, depth + 1);
        return copy;
    }

    private static object? FromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => FromJsonNumber(element),
        JsonValueKind.Array => element.EnumerateArray().Select(FromJson).ToList(),
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(p => p.Name, p => FromJson(p.Value), StringComparer.Ordinal),
        _ => null,
    };

    private static object FromJsonNumber(JsonElement element)
    {
        if (element.TryGetInt64(out long integer)) return integer;

        // An integer too large for Int64 - an unsigned property, say - survives exactly as a
        // decimal, where a double would round it. Anything with a fraction or an exponent stays a
        // double, as it always has.
        if (element.GetRawText().AsSpan().IndexOfAny('.', 'e', 'E') < 0 &&
            element.TryGetDecimal(out decimal large))
        {
            return large;
        }

        return element.GetDouble();
    }
}
