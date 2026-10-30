using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The JSON metadata of what the AI package serializes itself - the conversation snapshots and everything they hold,
/// the JSON Schema it builds for generative UI, a form's values, a tool's text result - generated at compile time
/// rather than reflected at run time, so a trimmed or AOT-compiled app keeps every member the JSON is written from.
/// </summary>
/// <remarks>
/// <para>
/// The options of the context are the defaults, which is what the package used when it wrote its own JSON without
/// options. The conversation snapshots are written with the options of
/// <see cref="BitAiConversationSerializerOptions.CreateJsonOptions"/> instead (web naming, enumerations as camelCase
/// strings, nulls left out), which take their metadata from this context through <see cref="BitAiSnapshotJson"/>, so
/// the JSON is the same as the reflection-based serializer wrote and is read back the same way.
/// </para>
/// <para>
/// A schema node and a metadata or extension value of the snapshots are dictionaries of loosely typed values; every
/// runtime type one of them can hold is named here (strings, booleans, numbers, dates, identifiers, nested
/// dictionaries, lists and arrays, JSON values), since the serializer looks an <see cref="object"/> up by the type it
/// holds. A value of a type of the app is resolved only through what the app adds to the options it hands the
/// serializer.
/// </para>
/// </remarks>
[JsonSerializable(typeof(BitAiConversationSnapshot))]
[JsonSerializable(typeof(BitAiPartSnapshot))]
[JsonSerializable(typeof(BitAiRole))]
[JsonSerializable(typeof(BitAiArtifactKind))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, object?>[]))]
[JsonSerializable(typeof(List<object?>))]
[JsonSerializable(typeof(object?[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonArray))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Uri))]
internal sealed partial class BitAiJsonContext : JsonSerializerContext;
