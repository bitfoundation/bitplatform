//+:cnd:noEmit
namespace Boilerplate.Server.Services;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, object>))]
//#if (captcha == "reCaptcha")
[JsonSerializable(typeof(GoogleRecaptchaVerificationResponse))]
//#endif
public partial class ServerJsonContext : JsonSerializerContext
{
}
