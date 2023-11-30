﻿//#if (sample == "Todo")
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
[JsonSerializable(typeof(List<UserDto>))]
//#if (sample == "Todo")
[JsonSerializable(typeof(TodoItemDto))]
[JsonSerializable(typeof(List<TodoItemDto>))]
//#elif (sample == "Admin")
[JsonSerializable(typeof(OverallAnalyticsStatsDataResponseDto))]
[JsonSerializable(typeof(List<ProductsCountPerCategoryResponseDto>))]
[JsonSerializable(typeof(List<ProductSaleStatResponseDto>))]
[JsonSerializable(typeof(List<ProductPercentagePerCategoryResponseDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
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
