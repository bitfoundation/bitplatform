using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// The result of one tool call.
/// </summary>
/// <remarks>
/// Carries both the structured <see cref="Value"/> that goes back to the model and the optional
/// <see cref="Text"/> that the UI shows, because the best rendering for a human is rarely the raw
/// JSON the model asked for.
/// </remarks>
public sealed record BitAiToolOutcome
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private BitAiToolOutcome() { }

    /// <summary>Whether the call succeeded.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>The structured result, sent back to the model.</summary>
    public JsonElement? Value { get; private init; }

    /// <summary>
    /// A human-readable rendering of the result, shown when no custom renderer is registered for the
    /// tool. Falls back to the JSON of <see cref="Value"/> when not set.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>The failure, when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public BitAiError? Error { get; private init; }

    /// <summary>Sources the tool consulted, merged into the message citation list.</summary>
    public IReadOnlyList<BitAiCitation>? Citations { get; init; }

    /// <summary>How long the call took.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Application-defined values carried through to custom tool-result renderers.</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    /// <summary>Creates a successful outcome carrying a JSON value.</summary>
    /// <param name="value">The structured result.</param>
    /// <param name="text">An optional human-readable rendering.</param>
    /// <returns>The outcome.</returns>
    public static BitAiToolOutcome Success(JsonElement value, string? text = null) =>
        new() { IsSuccess = true, Value = value, Text = text };

    /// <summary>Creates a successful outcome by serializing a typed value.</summary>
    /// <typeparam name="T">The result model.</typeparam>
    /// <param name="value">The result.</param>
    /// <param name="options">Serializer options. Defaults to web defaults.</param>
    /// <returns>The outcome.</returns>
    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    public static BitAiToolOutcome Success<T>(T value, JsonSerializerOptions? options = null) =>
        new() { IsSuccess = true, Value = JsonSerializer.SerializeToElement(value, options ?? WebOptions) };

    /// <summary>
    /// Creates a successful outcome by serializing a typed value with the JSON metadata of the app's own
    /// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>, so it is safe to trim.
    /// </summary>
    /// <typeparam name="T">The result model.</typeparam>
    /// <param name="value">The result.</param>
    /// <param name="typeInfo">The metadata of <typeparamref name="T"/>, whose options the JSON follows.</param>
    /// <returns>The outcome.</returns>
    public static BitAiToolOutcome Success<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return new() { IsSuccess = true, Value = JsonSerializer.SerializeToElement(value, typeInfo) };
    }

    /// <summary>Creates a successful outcome carrying only text.</summary>
    /// <param name="text">The result text.</param>
    /// <returns>The outcome.</returns>
    public static BitAiToolOutcome FromText(string text) =>
        new() { IsSuccess = true, Text = text, Value = JsonSerializer.SerializeToElement(text, BitAiJsonContext.Default.String) };

    /// <summary>Creates a failed outcome.</summary>
    /// <param name="error">The failure.</param>
    /// <returns>The outcome.</returns>
    public static BitAiToolOutcome Failure(BitAiError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new BitAiToolOutcome { IsSuccess = false, Error = error };
    }

    /// <summary>Creates a failed outcome from a message.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>The outcome.</returns>
    public static BitAiToolOutcome Failure(string message) =>
        Failure(new BitAiError(message) { Kind = BitAiErrorKind.Tool });

    /// <summary>
    /// The text to display for this outcome, falling back to the JSON of <see cref="Value"/>.
    /// </summary>
    /// <returns>Display text, or <see langword="null"/> when there is nothing to show.</returns>
    /// <remarks>
    /// The JSON of a value is written with only what JSON itself requires escaped, so an apostrophe, an ampersand or a
    /// word of a non-Latin script reads as it was written rather than as <c>'</c>. It is text to render as text -
    /// which a Razor expression does, encoding it - and never markup to write into a page as it is.
    /// </remarks>
    public string? GetDisplayText()
    {
        if (Text is not null) return Text;
        if (Error is not null) return Error.Message;
        return Value?.ValueKind switch
        {
            null or JsonValueKind.Undefined or JsonValueKind.Null => null,
            JsonValueKind.String => Value.Value.GetString(),
            _ => ToDisplayJson(Value.Value),
        };
    }

    // The raw text of a value serialized with the default encoder (SerializeToElement) has every character that is
    // sensitive in HTML, and everything outside ASCII, escaped. Written again with the relaxed encoder, a string keeps
    // only the escapes JSON needs (a quote, a backslash, a control character). No reflection is involved.
    private static string ToDisplayJson(JsonElement value)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            value.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
