//#if (sample == "Todo")
using BlazorWeb.Shared.Dtos.Todo;
//#elif (sample == "AdminPanel")
using BlazorWeb.Shared.Dtos.Categories;
using BlazorWeb.Shared.Dtos.Products;
using BlazorWeb.Shared.Dtos.Dashboard;
//#endif
using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
//#if (sample == "Todo")
[JsonSerializable(typeof(TodoItemDto))]
[JsonSerializable(typeof(List<TodoItemDto>))]
//#elif (sample == "AdminPanel")
[JsonSerializable(typeof(OverallAnalyticsStatsDataDto))]
[JsonSerializable(typeof(List<ProductsCountPerCategoryDto>))]
[JsonSerializable(typeof(List<ProductSaleStatDto>))]
[JsonSerializable(typeof(List<ProductPercentagePerCategoryDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(PagedResult<CategoryDto>))]
//#endif
[JsonSerializable(typeof(SignInRequestDto))]
[JsonSerializable(typeof(RefreshRequestDto))]
[JsonSerializable(typeof(TokenResponseDto))]
[JsonSerializable(typeof(SignUpRequestDto))]
[JsonSerializable(typeof(EditUserDto))]
[JsonSerializable(typeof(RestErrorInfo))]
[JsonSerializable(typeof(EmailConfirmedRequestDto))]
[JsonSerializable(typeof(SendConfirmationEmailRequestDto))]
[JsonSerializable(typeof(SendResetPasswordEmailRequestDto))]
[JsonSerializable(typeof(ResetPasswordRequestDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}
