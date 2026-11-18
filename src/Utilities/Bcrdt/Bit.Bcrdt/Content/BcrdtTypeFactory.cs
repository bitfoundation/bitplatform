namespace Bit.Bcrdt;

/// <summary>Creates collaborative type instances from their wire discriminator.</summary>
public static class BcrdtTypeFactory
{
    /// <summary>Creates an empty instance of the given kind.</summary>
    /// <exception cref="BcrdtFormatException">The kind is not recognised.</exception>
    /// <exception cref="ArgumentException">
    /// The kind is <see cref="BcrdtTypeKind.XmlElement"/>, which cannot exist without a name: create one
    /// with <c>new BcrdtXmlElement(name)</c>.
    /// </exception>
    public static BcrdtAbstractType Create(BcrdtTypeKind kind) => kind switch
    {
        BcrdtTypeKind.Map => new BcrdtMap(),
        BcrdtTypeKind.Array => new BcrdtArray(),
        BcrdtTypeKind.Text => new BcrdtText(),
        BcrdtTypeKind.Set => new BcrdtSet(),
        BcrdtTypeKind.Counter => new BcrdtCounter(),
        BcrdtTypeKind.XmlFragment => new BcrdtXmlFragment(),
        BcrdtTypeKind.XmlText => new BcrdtXmlText(),
        BcrdtTypeKind.XmlElement => throw new ArgumentException(
            "An XML element cannot be created without its name: use new BcrdtXmlElement(name).", nameof(kind)),
        BcrdtTypeKind.Untyped => new BcrdtUntypedType(),
        _ => throw BcrdtFormatException.UnknownTag("collaborative type", (int)kind),
    };
}
