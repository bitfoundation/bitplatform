//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(IdentityRequestDto))]
[JsonSerializable(typeof(SignInRequestDto))]
[JsonSerializable(typeof(SignInResponseDto))]
[JsonSerializable(typeof(TokenResponseDto))]
[JsonSerializable(typeof(RefreshRequestDto))]
[JsonSerializable(typeof(SignUpRequestDto))]
[JsonSerializable(typeof(EditUserDto))]
[JsonSerializable(typeof(SendEmailTokenRequestDto))]
[JsonSerializable(typeof(SendPhoneTokenRequestDto))]
[JsonSerializable(typeof(ConfirmEmailRequestDto))]
[JsonSerializable(typeof(ChangeEmailRequestDto))]
[JsonSerializable(typeof(ConfirmPhoneRequestDto))]
[JsonSerializable(typeof(ChangePhoneNumberRequestDto))]
[JsonSerializable(typeof(SendResetPasswordTokenRequestDto))]
[JsonSerializable(typeof(ResetPasswordRequestDto))]
[JsonSerializable(typeof(TwoFactorAuthRequestDto))]
[JsonSerializable(typeof(TwoFactorAuthResponseDto))]
[JsonSerializable(typeof(List<UserSessionDto>))]
public partial class IdentityJsonContext : JsonSerializerContext
{
}
