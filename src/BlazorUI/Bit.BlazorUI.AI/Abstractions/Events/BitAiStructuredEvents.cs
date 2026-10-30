namespace Bit.BlazorUI;

/// <summary>
/// More raw JSON for a structured-output part.
/// </summary>
/// <remarks>
/// Deltas are appended as text and re-parsed leniently on every update, so a truncated payload such
/// as <c>{"items":[{"name":"Ac</c> still yields a usable partial object. That is what lets a table
/// or a form fill in row by row while the model is still writing.
/// </remarks>
public sealed record BitAiStructuredDataDeltaEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="jsonDelta">Raw JSON text to append.</param>
    public BitAiStructuredDataDeltaEvent(string jsonDelta)
    {
        ArgumentNullException.ThrowIfNull(jsonDelta);
        JsonDelta = jsonDelta;
    }

    /// <summary>Raw JSON text to append.</summary>
    public string JsonDelta { get; }

    /// <summary>
    /// A name identifying the shape of the data, so the UI can pick a renderer registered for it.
    /// </summary>
    public string? SchemaName { get; init; }

    /// <inheritdoc />
    public override string Type => "structured.delta";
}

/// <summary>
/// A complete structured-output value, for transports that do not stream partial JSON.
/// </summary>
public sealed record BitAiStructuredDataEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="value">The structured value.</param>
    public BitAiStructuredDataEvent(JsonElement value) => Value = value;

    /// <summary>The structured value.</summary>
    public JsonElement Value { get; }

    /// <summary>A name identifying the shape of the data.</summary>
    public string? SchemaName { get; init; }

    /// <inheritdoc />
    public override string Type => "structured";
}

/// <summary>
/// The model asked for a registered component to be rendered with the supplied properties.
/// </summary>
/// <remarks>
/// The component name is resolved against the generative UI registry. Names that are not registered
/// are never rendered, which is what keeps model-produced UI from reaching arbitrary types.
/// </remarks>
public sealed record BitAiGenerativeUiEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="componentName">The registered component name.</param>
    /// <param name="props">The properties to bind.</param>
    public BitAiGenerativeUiEvent(string componentName, JsonElement props)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);
        ComponentName = componentName;
        Props = props;
    }

    /// <summary>The registered component name.</summary>
    public string ComponentName { get; }

    /// <summary>The properties to bind.</summary>
    public JsonElement Props { get; }

    /// <summary>
    /// Whether the properties are still being written, so the component can render a partial state.
    /// </summary>
    public bool IsPartial { get; init; }

    /// <inheritdoc />
    public override string Type => "generative_ui";
}

/// <summary>
/// More raw JSON for the properties of a generative UI part.
/// </summary>
public sealed record BitAiGenerativeUiDeltaEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="componentName">The registered component name.</param>
    /// <param name="propsDelta">Raw JSON text to append to the properties.</param>
    public BitAiGenerativeUiDeltaEvent(string componentName, string propsDelta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);
        ArgumentNullException.ThrowIfNull(propsDelta);
        ComponentName = componentName;
        PropsDelta = propsDelta;
    }

    /// <summary>The registered component name.</summary>
    public string ComponentName { get; }

    /// <summary>Raw JSON text to append to the properties.</summary>
    public string PropsDelta { get; }

    /// <inheritdoc />
    public override string Type => "generative_ui.delta";
}
