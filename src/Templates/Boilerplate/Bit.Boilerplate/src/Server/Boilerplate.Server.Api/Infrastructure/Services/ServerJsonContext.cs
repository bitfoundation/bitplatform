//+:cnd:noEmit
using Fido2NetLib;
using Boilerplate.Shared.Features.Statistics;
using Boilerplate.Server.Api.Features.Identity.Services;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NugetStatsDto))]
//#if (captcha == "reCaptcha")
[JsonSerializable(typeof(GoogleRecaptchaVerificationResponse))]
//#endif
[JsonSerializable(typeof(AuthenticatorResponse))]
public partial class ServerJsonContext : JsonSerializerContext
{
}
