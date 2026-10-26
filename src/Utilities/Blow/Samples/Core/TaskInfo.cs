using System.Text.Json.Serialization;

namespace Bit.Blow.Samples.Core;

/// <summary>The custom data of the Saving page's task nodes: who owns a task, and how long it takes.</summary>
public sealed class TaskInfo
{
    public string Owner { get; set; } = string.Empty;

    public int Hours { get; set; }
}

/// <summary>
/// The JSON contract of <see cref="TaskInfo"/>, generated at compile time: BlowSerializerOptions.MapNodeData takes the
/// type info rather than reflecting over the type, so a published WebAssembly app can trim the samples and still save
/// and load the data. It lives in a C# file because the JSON source generator cannot see types declared in a Razor
/// component.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TaskInfo))]
internal sealed partial class SampleJsonContext : JsonSerializerContext;
