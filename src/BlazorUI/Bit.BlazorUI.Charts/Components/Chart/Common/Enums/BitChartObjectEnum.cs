namespace Bit.BlazorUI;

/// <summary>
/// The base class for enums that can represent different types. We also use these
/// "enums" like Discriminated Unions to provide a type safe way of communicating with the
/// dynamic language JavaScript is.
/// <para>
/// De-/serialization is supported but only for the following types:
/// <see cref="int"/>, <see cref="double"/>, <see cref="string"/> and <see cref="bool"/>.
/// For the deserialization, the constructors with a single parameter of a supported type
/// are considered for instantiating the object enum.
/// </para>
/// <para>
/// When implementing an object enum, make sure to provide only private constructors
/// with the types that are allowed (DO NOT expose public constructors; expose meaningful
/// static factory methods instead). The actual enum values are static properties that pass
/// the correct value to the private constructor. Make these properties return new values
/// everytime so we don't create all the enum values even though we don't use them.
/// In the classic use case, we don't call many of these properties anyway and usually
/// only a few times. You can also have static factory methods that
/// create an instance of the object enum with the specified value as long as the parameter
/// type is supported. Also consider sealing your enum unless you have a specific reason not to.
/// </para>
/// </summary>
[Newtonsoft.Json.JsonConverter(typeof(JsonObjectEnumConverter))]
public abstract class BitChartObjectEnum : IEquatable<BitChartObjectEnum>
{
    /// <summary>
    /// Gets the <see cref="Type"/>s that are supported for serialization and deserialization.
    /// <see cref="BitChartObjectEnum"/> can contain objects of different types but you will get a
    /// <see cref="NotSupportedException"/> once you try to serialize (or deserialize) that
    /// <see cref="BitChartObjectEnum"/>.
    /// </summary>
    private static readonly Type[] _supportedSerializationTypes = new[]
    {
        typeof(int), typeof(double), typeof(string), typeof(bool)
    };

    /// <summary>
    /// Holds the actual value represented by this instance.
    /// </summary>
    internal object Value { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartObjectEnum"/>.
    /// </summary>
    /// <param name="value">The value this instance is supposed to represent.</param>
    protected BitChartObjectEnum(object value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));

        if (value is BitChartObjectEnum)
            throw new ArgumentException("The value cannot be an ObjectEnum. " +
                                        "Recursive ObjectEnums aren't allowed.");
    }

    /// <summary>
    /// Checks if a <see cref="Type"/> is in the list of supported serialization types.
    /// If this function returns <see langword="false"/>, de-/serialization will fail on
    /// <see cref="BitChartObjectEnum"/>s containing an instance of that <see cref="Type"/>
    /// (<paramref name="type"/>).
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    internal static bool IsSupportedSerializationType(Type type) =>
        _supportedSerializationTypes.Contains(type);

    /// <summary>
    /// Determines whether the specified object is considered equal to the current object.
    /// <para>
    /// <paramref name="obj"/> is considered to be equal to this instance if it..
    /// <list type="bullet">
    /// <item>is the same instance as this instance.</item>
    /// <item>is another <see cref="BitChartObjectEnum"/> with the same internal value.</item>
    /// <item>is the same value as the internal value of this <see cref="BitChartObjectEnum"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is considered to be equal to the current object;
    /// otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        if (obj is BitChartObjectEnum asEnum)
        {
            return Equals(asEnum);
        }

        return Value.Equals(obj);
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the other parameter; otherwise, false.
    /// </returns>
    public bool Equals(BitChartObjectEnum other) =>
        other != null &&
        Value.Equals(other.Value);

    /// <summary>
    /// Returns the hash code of the underlying value.
    /// </summary>
    /// <returns>The hash code of the underlying value.</returns>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns the <see cref="string"/> representation of the underlying object.
    /// Calls <see cref="object.ToString"/> on the underlying object.
    /// </summary>
    /// <returns>The <see cref="string"/> representation of the underlying object.</returns>
    public override string ToString() => Value.ToString();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static bool operator ==(BitChartObjectEnum left, BitChartObjectEnum right) =>
        EqualityComparer<object>.Default.Equals(left?.Value, right?.Value);

    public static bool operator !=(BitChartObjectEnum left, BitChartObjectEnum right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
