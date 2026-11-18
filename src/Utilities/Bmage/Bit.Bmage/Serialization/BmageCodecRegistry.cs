namespace Bit.Bmage;

/// <summary>The codecs in use, by CLR type for writing and by kind and name for reading.</summary>
internal sealed class BmageCodecRegistry
{
    private readonly Dictionary<Type, BmageCodec> _byType = [];
    private readonly Dictionary<(Type Kind, string Name), BmageCodec> _byName = [];

    public BmageCodecRegistry(IEnumerable<BmageCodec> codecs)
    {
        foreach (var codec in codecs) Add(codec);
    }

    /// <summary>Adds a codec, replacing any for the same type and any for the same name of the same kind.</summary>
    public void Add(BmageCodec codec)
    {
        // Whatever the new codec displaces, by name or by type, stops answering for both.
        if (_byName.TryGetValue((codec.Kind, codec.TypeName), out var sameName) && _byType.TryGetValue(sameName.Type, out var mapped) && ReferenceEquals(mapped, sameName))
            _byType.Remove(sameName.Type);
        if (_byType.TryGetValue(codec.Type, out var sameType) && _byName.TryGetValue((sameType.Kind, sameType.TypeName), out var named) && ReferenceEquals(named, sameType))
            _byName.Remove((sameType.Kind, sameType.TypeName));
        _byType[codec.Type] = codec;
        _byName[(codec.Kind, codec.TypeName)] = codec;
    }

    /// <summary>
    /// The codec for exactly the value's type. A subclass is not written as its base type, even of a matrix or curve
    /// operation: it may draw different pixels, and reading it back as the base would change the edit without a word.
    /// </summary>
    public BmageCodec Find(object value)
    {
        var type = value.GetType();
        if (_byType.TryGetValue(type, out var codec)) return codec;
        throw new NotSupportedException($"{type.Name} has no JSON codec. Register one with BmageJsonOptions.Add<{type.Name}>(...) so the document can be written and read back.");
    }

    /// <summary>True when a codec is registered for exactly this type.</summary>
    public bool Supports(Type type) => _byType.ContainsKey(type);

    public BmageCodec? Find(Type kind, string name)
    {
        if (_byName.TryGetValue((kind, name), out var codec)) return codec;
        // A codec registered for a concrete type answers for its own kind when asked for that type directly.
        return _byName.Values.FirstOrDefault(c => c.TypeName == name && kind.IsAssignableFrom(c.Type));
    }
}
