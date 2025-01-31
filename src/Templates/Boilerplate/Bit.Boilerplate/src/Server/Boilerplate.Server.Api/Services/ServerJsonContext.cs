//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Statistics;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NugetStatsDto))]
//#if (captcha == "reCaptcha")
[JsonSerializable(typeof(GoogleRecaptchaVerificationResponse))]
//#endif
[JsonSerializable(typeof(ProblemDetails))]
public partial class ServerJsonContext : JsonSerializerContext
{
}
