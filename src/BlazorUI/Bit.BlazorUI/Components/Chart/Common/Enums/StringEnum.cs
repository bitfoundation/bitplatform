namespace Bit.BlazorUI;

/// <summary>
/// The base class for enums that are meant to be serialized. They are more flexible
/// than normal C# enums (through type safe enum pattern).
/// <para>
/// When implementing a <see cref="StringEnum"/>, make sure to only implement a single
/// constructor that takes a single <see cref="string"/>. Make this constructor private!
/// The actual enum values are static properties that pass the correct value to the private
/// constructor. Make these properties return new values everytime so we don't create all
/// the enum values even though we don't use them. In the classic use case, we don't call
/// many of these properties anyway and usually only a few times.
/// In the rare case that you need a <see cref="StringEnum"/> that can contain any
/// <see cref="string"/> value, expose a static factory method but don't make the constructor
/// public. Also consider sealing your enum unless you have a specific reason not to.
/// </para>
/// </summary>
[Newtonsoft.Json.JsonConverter(typeof(JsonStringEnumConverter))]
public abstract class StringEnum : IEquatable<StringEnum>
{
    private readonly string _value;

    /// <summary>
    /// Creates a new instance of <see cref="StringEnum"/>.
    /// </summary>
    /// <param name="stringRep">The <see cref="string"/> this instance should represent.</param>
    protected StringEnum(string stringRep)
    {
        _value = stringRep ?? throw new ArgumentNullException(nameof(stringRep));
    }

    /// <summary>
    /// Determines whether the specified object is considered equal to the current object.
    /// <para>
    /// <paramref name="obj"/> is considered to be equal to this instance if it..
    /// <list type="bullet">
    /// <item>is the same instance as this instance.</item>
    /// <item>is another <see cref="StringEnum"/> with the same internal <see cref="string"/>
    /// value.</item>
    /// <item>is the same <see cref="string"/> value as the internal <see cref="string"/> value
    /// of this <see cref="StringEnum"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is considered to be equal to the current
    /// object; otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        if (obj is StringEnum asEnum)
        {
            return Equals(asEnum);
        }

        if (obj is string asString)
        {
            return _value == asString;
        }

        return false;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the other parameter;
    /// otherwise, false.</returns>
    public bool Equals(StringEnum other) =>
        other != null &&
        _value == other._value;

    /// <summary>
    /// Returns the hash code of the underlying <see cref="string"/> value.
    /// </summary>
    /// <returns>The hash code of the underlying <see cref="string"/> value.</returns>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Returns the underlying <see cref="string"/> value.
    /// </summary>
    /// <returns>The underlying <see cref="string"/> value.</returns>
    public override string ToString() => _value;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static bool operator ==(StringEnum left, StringEnum right) =>
        left?._value == right?._value;

    public static bool operator !=(StringEnum left, StringEnum right) =>
        left?._value != right?._value;

    public static explicit operator string(StringEnum stringEnum) => stringEnum._value;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
