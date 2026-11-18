namespace Bit.Bmage;

/// <summary>Options for <see cref="BmageJson"/>: how images are referred to, layout, and codecs for custom types.</summary>
public sealed class BmageJsonOptions
{
    /// <summary>The built-in codecs, with no image handling: documents without images round-trip with these.</summary>
    public static BmageJsonOptions Default { get; } = new() { _frozen = true };

    private bool _frozen;

    public BmageJsonOptions() => Codecs = new BmageCodecRegistry(BmageBuiltInCodecs.All);

    private BmageJsonOptions(BmageCodecRegistry codecs) => Codecs = codecs;

    /// <summary>
    /// A copy with the same codecs, the custom ones included, and the given image handling, for a reader or writer that
    /// stores the images itself, such as <see cref="BmageProject"/>. The caller's options are left as they are.
    /// </summary>
    internal BmageJsonOptions WithImages(Func<BmageImageBuffer, string>? references, Func<string, BmageImageBuffer?>? resolver) => new(Codecs)
    {
        ImageReferences = references,
        ImageResolver = resolver,
        Indented = Indented,
    };

    /// <summary>
    /// Chooses the reference written for an image an annotation, layer or mask uses: a file name, an upload id, a
    /// content hash. Called once per occurrence; return the same reference for the same image to store it once.
    /// </summary>
    public Func<BmageImageBuffer, string>? ImageReferences
    {
        get;
        set { ThrowIfFrozen(); field = value; }
    }

    /// <summary>Supplies the image for a reference written by <see cref="ImageReferences"/>, or null when it is unknown.</summary>
    public Func<string, BmageImageBuffer?>? ImageResolver
    {
        get;
        set { ThrowIfFrozen(); field = value; }
    }

    /// <summary>Indent the JSON for people to read. Default false.</summary>
    public bool Indented
    {
        get;
        set { ThrowIfFrozen(); field = value; }
    }

    internal BmageCodecRegistry Codecs { get; }

    /// <summary>
    /// Registers how a custom operation, annotation, layer or mask is written and read. <paramref name="type"/> is the
    /// <c>"type"</c> it is stored under; namespacing it, as in <c>"acme.duotone"</c>, keeps it clear of built-in names.
    /// </summary>
    /// <example>
    /// <code>
    /// options.Add&lt;DuotoneOperation&gt;("acme.duotone",
    ///     (writer, duotone) => writer.Write("shadow", duotone.Shadow).Write("highlight", duotone.Highlight),
    ///     reader => new DuotoneOperation(reader.GetColor("shadow"), reader.GetColor("highlight")));
    /// </code>
    /// </example>
    public BmageJsonOptions Add<T>(string type, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : class
    {
        ThrowIfFrozen();
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(read);
        var kind = typeof(IBmageImageOperation).IsAssignableFrom(typeof(T)) ? typeof(IBmageImageOperation)
            : typeof(BmageAnnotation).IsAssignableFrom(typeof(T)) ? typeof(BmageAnnotation)
            : typeof(BmageLayer).IsAssignableFrom(typeof(T)) ? typeof(BmageLayer)
            : typeof(BmageMask).IsAssignableFrom(typeof(T)) ? typeof(BmageMask)
            : throw new ArgumentException($"{typeof(T).Name} is not an operation, annotation, layer or mask.", nameof(T));
        Codecs.Add(new BmageCodec(kind, typeof(T), type, (w, value) => write(w, (T)value), r => read(r)));
        return this;
    }

    private void ThrowIfFrozen()
    {
        if (_frozen) throw new InvalidOperationException("BmageJsonOptions.Default is shared and cannot be changed. Create new BmageJsonOptions instead.");
    }
}
