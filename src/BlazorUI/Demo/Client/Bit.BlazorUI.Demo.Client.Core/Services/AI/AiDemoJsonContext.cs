using System.Text.Json;

namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The JSON metadata of the arguments and results of the demo's tools, generated at compile time so the tools are safe
/// to trim (see <see cref="AiDemoCatalog.CreateTools"/>).
/// </summary>
/// <remarks>
/// The web defaults - camelCase names, read case-insensitively - are what the scripted transports write and what the
/// custom renderers read back (<c>"hits"</c>, <c>"title"</c>, <c>"score"</c>), and what the reflection-based overloads
/// of <see cref="BitAiTool"/> would have used.
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AiDemoCatalog.WeatherArgs))]
[JsonSerializable(typeof(AiDemoCatalog.WeatherResult))]
[JsonSerializable(typeof(AiDemoCatalog.SearchArgs))]
[JsonSerializable(typeof(AiDemoCatalog.SearchResult))]
[JsonSerializable(typeof(AiDemoCatalog.DeleteArgs))]
[JsonSerializable(typeof(AiDemoCatalog.DeleteResult))]
public partial class AiDemoJsonContext : JsonSerializerContext
{
}
