//+:cnd:noEmit
//#if (sample == "Todo")
using Boilerplate.Shared.Dtos.Todo;
//#elif (sample == "Admin")
using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Shared.Dtos.Dashboard;
using Boilerplate.Shared.Dtos.Products;
//#endif
//#if (notification == true)
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Shared.Dtos.Home;
//#endif

namespace Boilerplate.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(RestErrorInfo))]
[JsonSerializable(typeof(NugetStatsDto))]
//#if (notification == true)
[JsonSerializable(typeof(DeviceInstallationDto))]
//#endif
//#if (sample == "Todo")
[JsonSerializable(typeof(TodoItemDto))]
[JsonSerializable(typeof(PagedResult<TodoItemDto>))]
[JsonSerializable(typeof(List<TodoItemDto>))]
//#elif (sample == "Admin")
[JsonSerializable(typeof(List<ProductsCountPerCategoryResponseDto>))]
[JsonSerializable(typeof(OverallAnalyticsStatsDataResponseDto))]
[JsonSerializable(typeof(List<ProductPercentagePerCategoryResponseDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(PagedResult<CategoryDto>))]
[JsonSerializable(typeof(List<CategoryDto>))]
//#endif
public partial class AppJsonContext : JsonSerializerContext
{
}
