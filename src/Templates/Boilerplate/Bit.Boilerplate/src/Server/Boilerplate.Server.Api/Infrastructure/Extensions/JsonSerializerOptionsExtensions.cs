using FluentEmail.Core;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace System.Text.Json;

public static class JsonSerializerOptionsExtensions
{
    public static void ApplyDefaultOptions(this JsonSerializerOptions options)
    {
        // Changes in this file should be applied on AppJsonContext.cs, IdentityJsonContext.cs and ServerJsonContext.cs as well,
        // so source generators can use the same options.

        options.AllowTrailingCommas = true;
        options.PropertyNameCaseInsensitive = true;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        options.TypeInfoResolverChain.AddRange([AppJsonContext.Default,
            IdentityJsonContext.Default,
            ServerJsonContext.Default]);
    }
}
