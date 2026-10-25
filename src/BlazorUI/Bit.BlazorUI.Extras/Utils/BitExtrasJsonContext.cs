using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The JSON metadata of what the Extras components serialize themselves, generated at compile time rather than
/// reflected at run time, so a trimmed or AOT-compiled app keeps every member the JSON is written from. The options are
/// the defaults the reflection-based serializer used, so the JSON is the same.
/// </summary>
/// <remarks>
/// A PDF page's canvas operations are lists of loosely typed values; every runtime type one of them can hold is named
/// here (strings, booleans, numbers, number arrays, nested operations), since the serializer looks an
/// <see cref="object"/> up by the type it holds.
/// </remarks>
[JsonSerializable(typeof(BitRichTextEditorSetupOptions))]
[JsonSerializable(typeof(BitRichTextEditorPolicyPayload))]
[JsonSerializable(typeof(List<object?[]>))]
[JsonSerializable(typeof(object?[]))]
[JsonSerializable(typeof(double[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(int))]
internal sealed partial class BitExtrasJsonContext : JsonSerializerContext;
