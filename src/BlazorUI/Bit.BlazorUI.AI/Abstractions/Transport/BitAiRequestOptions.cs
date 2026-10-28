namespace Bit.BlazorUI;

/// <summary>
/// How the model should be constrained to produce a particular output shape.
/// </summary>
/// <param name="Kind">Whether the response is free text, any JSON, or JSON matching a schema.</param>
/// <param name="Schema">The JSON Schema, when <paramref name="Kind"/> is a schema-constrained format.</param>
/// <param name="Name">A name for the schema, which some providers require.</param>
public readonly record struct BitAiResponseFormat(BitAiResponseFormatKind Kind, JsonElement? Schema = null, string? Name = null)
{
    /// <summary>Free-form text. The default.</summary>
    public static readonly BitAiResponseFormat Text = new(BitAiResponseFormatKind.Text);

    /// <summary>Any valid JSON value.</summary>
    public static readonly BitAiResponseFormat Json = new(BitAiResponseFormatKind.Json);

    /// <summary>Creates a schema-constrained format.</summary>
    /// <param name="schema">The JSON Schema the response must satisfy.</param>
    /// <param name="name">A name for the schema.</param>
    /// <returns>The response format.</returns>
    public static BitAiResponseFormat FromSchema(JsonElement schema, string? name = null) =>
        new(BitAiResponseFormatKind.JsonSchema, schema, name);
}

/// <summary>
/// The kinds of output constraint a request can ask for.
/// </summary>
public enum BitAiResponseFormatKind
{
    /// <summary>Free-form text.</summary>
    Text = 0,

    /// <summary>Any valid JSON value.</summary>
    Json = 1,

    /// <summary>JSON matching a supplied schema.</summary>
    JsonSchema = 2,
}

/// <summary>
/// Whether and how the model may call tools.
/// </summary>
public readonly record struct BitAiToolChoice
{
    /// <summary>The model decides. The default.</summary>
    public static readonly BitAiToolChoice Auto = new("auto", null);

    /// <summary>The model must not call a tool.</summary>
    public static readonly BitAiToolChoice None = new("none", null);

    /// <summary>The model must call some tool.</summary>
    public static readonly BitAiToolChoice Required = new("required", null);

    private BitAiToolChoice(string mode, string? toolName)
    {
        Mode = mode;
        ToolName = toolName;
    }

    /// <summary>The choice mode: <c>auto</c>, <c>none</c>, <c>required</c>, or <c>specific</c>.</summary>
    public string Mode { get; }

    /// <summary>The tool that must be called, when <see cref="Mode"/> is <c>specific</c>.</summary>
    public string? ToolName { get; }

    /// <summary>Requires the model to call one named tool.</summary>
    /// <param name="toolName">The tool to call.</param>
    /// <returns>The tool choice.</returns>
    public static BitAiToolChoice Specific(string toolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        return new BitAiToolChoice("specific", toolName);
    }
}

/// <summary>
/// Generation settings that the UI can expose and a transport passes through to its provider.
/// </summary>
/// <remarks>
/// Every setting is optional, and a transport is free to ignore any it cannot express. Nothing here
/// is required for the framework to work, so a minimal transport can ignore this type entirely.
/// </remarks>
public sealed record BitAiRequestOptions
{
    /// <summary>The model to use.</summary>
    public string? ModelId { get; init; }

    /// <summary>The provider to route to, for hosts that front several.</summary>
    public string? ProviderId { get; init; }

    /// <summary>Sampling temperature.</summary>
    public double? Temperature { get; init; }

    /// <summary>Nucleus sampling cutoff.</summary>
    public double? TopP { get; init; }

    /// <summary>Maximum tokens to generate.</summary>
    public long? MaxOutputTokens { get; init; }

    /// <summary>Sequences that stop generation.</summary>
    public IReadOnlyList<string>? StopSequences { get; init; }

    /// <summary>A seed for reproducible sampling, where the provider supports it.</summary>
    public long? Seed { get; init; }

    /// <summary>The output shape the model should produce.</summary>
    public BitAiResponseFormat? ResponseFormat { get; init; }

    /// <summary>Whether and how the model may call tools.</summary>
    public BitAiToolChoice? ToolChoice { get; init; }

    /// <summary>A system prompt prepended to the conversation.</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Provider-specific settings that have no first-class field. Transports read the keys they know
    /// and ignore the rest.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extra { get; init; }
}
