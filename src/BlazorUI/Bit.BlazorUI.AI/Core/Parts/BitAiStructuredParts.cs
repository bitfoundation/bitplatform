using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// A structured JSON value produced by the model, rendered by a schema-specific renderer.
/// </summary>
/// <remarks>
/// Holds the raw text as well as the parsed value so that a partially written payload still yields
/// something to render. A table bound to this part fills in row by row as the model writes, rather
/// than appearing all at once when the last brace arrives.
/// </remarks>
public sealed class BitAiStructuredDataPart : BitAiMessagePart, IBitAiStreamablePart
{
    // Only touched under _gate: deltas arrive on the transport's thread while renderers read Value.
    private readonly StringBuilder _raw = new(256);
    private readonly object _gate = new();
    private JsonElement? _value;
    private JsonElement? _partial;
    private bool _partialStale = true;
    private volatile bool _isStreaming = true;

    /// <summary>Creates an empty, streaming structured part.</summary>
    /// <param name="schemaName">A name identifying the shape, used to select a renderer.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiStructuredDataPart(string? schemaName = null, string? partId = null) : base(partId) =>
        SchemaName = schemaName;

    /// <summary>Creates a completed structured part.</summary>
    /// <param name="value">The value.</param>
    /// <param name="schemaName">A name identifying the shape.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiStructuredDataPart(JsonElement value, string? schemaName = null, string? partId = null) : base(partId)
    {
        SchemaName = schemaName;
        _value = value;
        _isStreaming = false;
        _raw.Append(value.GetRawText());
    }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.StructuredData;

    /// <summary>A name identifying the shape, used to select a renderer.</summary>
    public string? SchemaName { get; set; }

    /// <inheritdoc />
    public bool IsStreaming => _isStreaming;

    /// <summary>The raw JSON text received so far.</summary>
    public string RawJson
    {
        get
        {
            lock (_gate) return _raw.ToString();
        }
    }

    /// <summary>The parsed value once complete, otherwise a lenient parse of what has arrived.</summary>
    public JsonElement? Value
    {
        get
        {
            lock (_gate)
            {
                if (_value is not null) return _value;
                if (!_partialStale) return _partial;
                _partial = BitAiPartialJson.TryParse(_raw.ToString());
                _partialStale = false;
                return _partial;
            }
        }
    }

    /// <summary>Appends raw JSON text.</summary>
    /// <param name="delta">The fragment to append.</param>
    public void Append(string? delta)
    {
        if (string.IsNullOrEmpty(delta)) return;
        lock (_gate)
        {
            _raw.Append(delta);
            _partialStale = true;
        }

        NotifyChanged();
    }

    /// <summary>
    /// Deserializes the current value into a typed model, tolerating a partial payload.
    /// </summary>
    /// <typeparam name="T">The target model. Use nullable members: fields not yet written are absent.</typeparam>
    /// <param name="options">Serializer options. Defaults to web defaults.</param>
    /// <returns>The model, or <see langword="null"/> when nothing parseable has arrived.</returns>
    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    public T? As<T>(JsonSerializerOptions? options = null)
    {
        if (Value is not { } value) return default;
        try
        {
            return value.Deserialize<T>(options ?? WebOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Deserializes the current value into a typed model with the JSON metadata of the app's own
    /// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>, tolerating a partial payload. Safe to trim.
    /// </summary>
    /// <typeparam name="T">The target model. Use nullable members: fields not yet written are absent.</typeparam>
    /// <param name="typeInfo">The metadata of <typeparamref name="T"/>, whose options the reading follows.</param>
    /// <returns>The model, or <see langword="null"/> when nothing parseable has arrived.</returns>
    public T? As<T>(JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        if (Value is not { } value) return default;
        try
        {
            return value.Deserialize(typeInfo);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>Marks the value complete and reparses it strictly.</summary>
    /// <param name="value">The final value, or <see langword="null"/> to parse the accumulated text.</param>
    public void Complete(JsonElement? value = null)
    {
        lock (_gate)
        {
            _value = value ?? BitAiPartialJson.TryParseComplete(_raw.ToString()) ?? Value;
            _isStreaming = false;
        }

        NotifyChanged();
    }

    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
}

/// <summary>
/// A component the model chose, bound to properties the model produced.
/// </summary>
/// <remarks>
/// The component name is resolved against the generative UI registry at render time. A name that is
/// not registered renders a fallback rather than anything the model named, which is what keeps
/// model output from reaching arbitrary types.
/// </remarks>
public sealed class BitAiGenerativeUiPart : BitAiMessagePart, IBitAiStreamablePart
{
    // Only touched under _gate: props stream in on the transport's thread while the view binds them.
    private readonly StringBuilder _raw = new(256);
    private readonly object _gate = new();
    private JsonElement? _props;
    private JsonElement? _partial;
    private bool _partialStale = true;
    private volatile bool _isStreaming = true;

    /// <summary>Creates a generative UI part.</summary>
    /// <param name="componentName">The registered component name.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiGenerativeUiPart(string componentName, string? partId = null) : base(partId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);
        ComponentName = componentName;
    }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.GenerativeUi;

    /// <summary>The registered component name.</summary>
    public string ComponentName { get; }

    /// <inheritdoc />
    public bool IsStreaming => _isStreaming;

    /// <summary>The raw JSON property text received so far.</summary>
    public string RawProps
    {
        get
        {
            lock (_gate) return _raw.ToString();
        }
    }

    /// <summary>The properties once complete, otherwise a lenient parse of what has arrived.</summary>
    public JsonElement? Props
    {
        get
        {
            lock (_gate)
            {
                if (_props is not null) return _props;
                if (!_partialStale) return _partial;
                _partial = BitAiPartialJson.TryParse(_raw.ToString());
                _partialStale = false;
                return _partial;
            }
        }
    }

    /// <summary>Appends raw JSON property text.</summary>
    /// <param name="delta">The fragment to append.</param>
    public void AppendProps(string? delta)
    {
        if (string.IsNullOrEmpty(delta)) return;
        lock (_gate)
        {
            _raw.Append(delta);
            _partialStale = true;
        }

        NotifyChanged();
    }

    /// <summary>Sets the complete properties.</summary>
    /// <param name="props">The properties.</param>
    public void SetProps(JsonElement props)
    {
        lock (_gate)
        {
            _props = props;
            _raw.Clear();
            _raw.Append(props.GetRawText());
            _partialStale = true;
        }

        NotifyChanged();
    }

    /// <summary>Marks the properties complete.</summary>
    public void Complete()
    {
        lock (_gate)
        {
            if (!_isStreaming) return;
            _props ??= BitAiPartialJson.TryParseComplete(_raw.ToString()) ?? Props;
            _isStreaming = false;
        }

        NotifyChanged();
    }
}
