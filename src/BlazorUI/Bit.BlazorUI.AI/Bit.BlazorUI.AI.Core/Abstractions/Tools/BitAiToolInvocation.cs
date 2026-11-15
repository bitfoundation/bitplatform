using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// Incremental progress reported by a running tool.
/// </summary>
/// <param name="Fraction">Completion in the range 0..1, or <see langword="null"/> when indeterminate.</param>
/// <param name="Message">A short status line, for example the current sub-step.</param>
public readonly record struct BitAiToolProgress(double? Fraction, string? Message = null);

/// <summary>
/// Everything a tool needs to execute one call.
/// </summary>
public sealed class BitAiToolInvocation
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Creates an invocation.</summary>
    /// <param name="toolCallId">Identifier of the call, matching the tool-call part in the message.</param>
    /// <param name="toolName">The tool being called.</param>
    /// <param name="arguments">The arguments the model produced.</param>
    public BitAiToolInvocation(string toolCallId, string toolName, JsonElement arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ToolCallId = toolCallId;
        ToolName = toolName;
        Arguments = arguments;
    }

    /// <summary>Identifier of the call, matching the tool-call part in the message.</summary>
    public string ToolCallId { get; }

    /// <summary>The tool being called.</summary>
    public string ToolName { get; }

    /// <summary>The arguments the model produced, already parsed.</summary>
    public JsonElement Arguments { get; }

    /// <summary>Identifier of the conversation the call belongs to.</summary>
    public string? ConversationId { get; init; }

    /// <summary>Identifier of the message the call belongs to.</summary>
    public string? MessageId { get; init; }

    /// <summary>Scoped services, so tools can resolve dependencies without capturing them.</summary>
    public IServiceProvider? Services { get; init; }

    /// <summary>
    /// Where to report progress. Never <see langword="null"/> for calls the framework dispatches, so
    /// implementations can report unconditionally.
    /// </summary>
    public IProgress<BitAiToolProgress> Progress { get; init; } = NullProgress.Instance;

    /// <summary>Application-defined values threaded through from the caller.</summary>
    public IReadOnlyDictionary<string, object?>? Context { get; init; }

    /// <summary>
    /// Deserializes <see cref="Arguments"/> into a typed model.
    /// </summary>
    /// <typeparam name="T">The argument model.</typeparam>
    /// <param name="options">Serializer options. Defaults to case-insensitive property matching.</param>
    /// <returns>The deserialized arguments, or <see langword="null"/> when the payload is null.</returns>
    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    public T? GetArguments<T>(JsonSerializerOptions? options = null) =>
        Arguments.ValueKind == JsonValueKind.Undefined ? default : Arguments.Deserialize<T>(options ?? DefaultOptions);

    /// <summary>
    /// Deserializes <see cref="Arguments"/> into a typed model with the JSON metadata of the app's own
    /// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>, so it is safe to trim.
    /// </summary>
    /// <typeparam name="T">The argument model.</typeparam>
    /// <param name="typeInfo">
    /// The metadata of <typeparamref name="T"/>. Property names match as its options say: give the context
    /// <c>PropertyNameCaseInsensitive</c> (or the web defaults) to match the way the other overload does.
    /// </param>
    /// <returns>The deserialized arguments, or <see langword="null"/> when the payload is null.</returns>
    public T? GetArguments<T>(JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return Arguments.ValueKind == JsonValueKind.Undefined ? default : Arguments.Deserialize(typeInfo);
    }

    private sealed class NullProgress : IProgress<BitAiToolProgress>
    {
        public static readonly NullProgress Instance = new();

        public void Report(BitAiToolProgress value) { }
    }
}
