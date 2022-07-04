using System.Text.Json.Serialization;
using Bit.BlazorUI.Playground.Shared.Dtos.DataGridDemo;

namespace Bit.BlazorUI.Playground.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FoodRecallQueryResult))]
[JsonSerializable(typeof(Meta))]
[JsonSerializable(typeof(FoodRecall))]
[JsonSerializable(typeof(Results))]
[JsonSerializable(typeof(Openfda))]
public partial class PlaygroundJsonContext : JsonSerializerContext
{
  
}
