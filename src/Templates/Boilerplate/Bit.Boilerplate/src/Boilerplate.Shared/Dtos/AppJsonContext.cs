//#if (sample == "Todo")
using Boilerplate.Shared.Dtos.Todo;
//#elif (sample == "Admin")
using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Shared.Dtos.Dashboard;
using Boilerplate.Shared.Dtos.Products;
//#endif
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(UserDto))]
//#if (sample == "Todo")
[JsonSerializable(typeof(TodoItemDto))]
[JsonSerializable(typeof(PagedResult<TodoItemDto>))]
//#elif (sample == "Admin")
[JsonSerializable(typeof(ProductsCountPerCategoryResponseDto))]
[JsonSerializable(typeof(OverallAnalyticsStatsDataResponseDto))]
[JsonSerializable(typeof(ProductSaleStatResponseDto))]
[JsonSerializable(typeof(ProductPercentagePerCategoryResponseDto))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(PagedResult<CategoryDto>))]
//#endif
[JsonSerializable(typeof(SignInRequestDto))]
[JsonSerializable(typeof(TokenResponseDto))]
[JsonSerializable(typeof(RefreshRequestDto))]
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
