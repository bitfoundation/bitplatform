using Bit.BlazorUI.Demo.Shared.Dtos.QuickGridDemo;
using Bit.BlazorUI.Demo.Shared.Dtos.SiteSearch;

namespace Bit.BlazorUI.Demo.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FoodRecallQueryResult))]
[JsonSerializable(typeof(Meta))]
[JsonSerializable(typeof(FoodRecall))]
[JsonSerializable(typeof(Results))]
[JsonSerializable(typeof(Openfda))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(PagedResult<CategoryOrProductDto>))]
[JsonSerializable(typeof(RestErrorInfo))]
[JsonSerializable(typeof(List<SiteSearchSectionDto>))]
public partial class AppJsonContext : JsonSerializerContext
{
}
