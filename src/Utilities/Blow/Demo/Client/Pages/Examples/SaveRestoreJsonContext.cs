using System.Text.Json.Serialization;

namespace Bit.Blow.Demo.Client.Pages.Examples;

/// <summary>The save-and-restore example's custom node data: who owns a task, and how long it takes.</summary>
public sealed class TaskInfo
{
    public string Owner { get; set; } = "";
    public int Hours { get; set; }
}

/// <summary>
/// The JSON contract of <see cref="TaskInfo"/>, generated at compile time so the demo stays trimmable:
/// BlowSerializerOptions.MapNodeData takes the type info rather than reflecting over the type. (It lives in a C# file
/// of its own because the JSON source generator cannot see types declared inside a Razor component.)
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TaskInfo))]
internal sealed partial class SaveRestoreJsonContext : JsonSerializerContext;
