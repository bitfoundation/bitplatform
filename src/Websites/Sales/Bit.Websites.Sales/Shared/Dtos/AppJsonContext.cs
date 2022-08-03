using Bit.Websites.Sales.Shared.Dtos.ContactUs;

namespace Bit.Websites.Sales.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RestExceptionPayload))]
[JsonSerializable(typeof(ContactUsDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}
