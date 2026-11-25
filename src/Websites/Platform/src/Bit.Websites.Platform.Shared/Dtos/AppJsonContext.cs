using Bit.Websites.Platform.Shared.Dtos.ContactUs;

namespace Bit.Websites.Platform.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(RestErrorInfo))]
[JsonSerializable(typeof(ContactUsDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}
