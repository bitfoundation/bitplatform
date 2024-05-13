//+:cnd:noEmit
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
[JsonSerializable(typeof(List<TodoItemDto>))]
//#elif (sample == "Admin")
[JsonSerializable(typeof(List<ProductsCountPerCategoryResponseDto>))]
[JsonSerializable(typeof(OverallAnalyticsStatsDataResponseDto))]
[JsonSerializable(typeof(List<ProductSaleStatResponseDto>))]
[JsonSerializable(typeof(List<ProductPercentagePerCategoryResponseDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(PagedResult<CategoryDto>))]
[JsonSerializable(typeof(List<CategoryDto>))]
//#endif
[JsonSerializable(typeof(SignInRequestDto))]
[JsonSerializable(typeof(SignInResponseDto))]
[JsonSerializable(typeof(TokenResponseDto))]
[JsonSerializable(typeof(RefreshRequestDto))]
[JsonSerializable(typeof(SignUpRequestDto))]
[JsonSerializable(typeof(EditUserDto))]
[JsonSerializable(typeof(RestErrorInfo))]
[JsonSerializable(typeof(SendEmailTokenRequestDto))]
[JsonSerializable(typeof(SendPhoneNumberTokenRequestDto))]
[JsonSerializable(typeof(ConfirmEmailRequestDto))]
[JsonSerializable(typeof(ChangeEmailRequestDto))]
[JsonSerializable(typeof(ConfirmPhoneNumberRequestDto))]
[JsonSerializable(typeof(ChangePhoneNumberRequestDto))]
[JsonSerializable(typeof(SendResetPasswordTokenRequestDto))]
[JsonSerializable(typeof(ResetPasswordRequestDto))]
[JsonSerializable(typeof(TwoFactorAuthRequestDto))]
[JsonSerializable(typeof(TwoFactorAuthResponseDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}
