using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The JSON metadata of the layouts of <see cref="BitVirtualKeyboard"/>, generated at compile time so a trimmed or
/// AOT-compiled app keeps every member a layout is read and written with.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(BitVirtualKeyboardLayout))]
[JsonSerializable(typeof(List<BitVirtualKeyboardKey>))]
[JsonSerializable(typeof(BitVirtualKeyboardRowAlign))]
internal sealed partial class BitVirtualKeyboardJsonContext : JsonSerializerContext;