﻿using System.Text.Json.Serialization;
using Bit.Websites.Platform.Shared.Dtos.ContactUs;
using Bit.Websites.Platform.Shared.Dtos.SupportPackage;

namespace Bit.Websites.Platform.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ContactUsDto))]
[JsonSerializable(typeof(BuyPackageDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}
