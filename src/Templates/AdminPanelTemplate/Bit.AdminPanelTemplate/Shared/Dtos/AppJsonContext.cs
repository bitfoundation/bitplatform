using AdminPanelTemplate.Shared.Dtos.Account;
using AdminPanelTemplate.Shared.Dtos.Categories;
using AdminPanelTemplate.Shared.Dtos.Dashboard;
using AdminPanelTemplate.Shared.Dtos.Products;

namespace AdminPanelTemplate.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(PagedInputDto))]
[JsonSerializable(typeof(OverallAnalyticsStatsDataDto))]
[JsonSerializable(typeof(List<ProductsCountPerCategoryDto>))]
[JsonSerializable(typeof(List<ProductSaleStatDto>))]
[JsonSerializable(typeof(List<ProductPercentagePerCategoryDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(PagedResultDto<ProductDto>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(PagedResultDto<CategoryDto>))]
[JsonSerializable(typeof(SignInRequestDto))]
[JsonSerializable(typeof(SignInResponseDto))]
[JsonSerializable(typeof(SignUpRequestDto))]
[JsonSerializable(typeof(EditUserDto))]
[JsonSerializable(typeof(RestExceptionPayload))]
[JsonSerializable(typeof(EmailConfirmedRequestDto))]
[JsonSerializable(typeof(SendConfirmationEmailRequestDto))]
[JsonSerializable(typeof(SendResetPasswordEmailRequestDto))]
[JsonSerializable(typeof(ResetPasswordRequestDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}
