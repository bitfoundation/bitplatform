namespace Bit.Bcrdt;

/// <summary>Discriminates the built-in collaborative types on the wire.</summary>
/// <remarks>These values are persisted; never renumber an existing entry.</remarks>
public enum BcrdtTypeKind : byte
{
    /// <summary>A <see cref="BcrdtMap"/>.</summary>
    Map = 0,
    /// <summary>A <see cref="BcrdtArray"/>.</summary>
    Array = 1,
    /// <summary>A <see cref="BcrdtText"/>.</summary>
    Text = 2,
    /// <summary>A <see cref="BcrdtSet"/>.</summary>
    Set = 3,
    /// <summary>A root type whose kind is not yet known.</summary>
    Untyped = 4,
    /// <summary>A <see cref="BcrdtCounter"/>.</summary>
    Counter = 5,
    /// <summary>A <see cref="BcrdtXmlFragment"/>.</summary>
    XmlFragment = 6,
    /// <summary>A <see cref="BcrdtXmlElement"/>. The only kind whose payload carries more than its number: the element's name follows it.</summary>
    XmlElement = 7,
    /// <summary>A <see cref="BcrdtXmlText"/>.</summary>
    XmlText = 8,
}
