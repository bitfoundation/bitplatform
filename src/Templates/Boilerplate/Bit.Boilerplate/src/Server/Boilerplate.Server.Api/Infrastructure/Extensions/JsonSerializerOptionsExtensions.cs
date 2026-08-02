using FluentEmail.Core;

namespace System.Text.Json;

public static class JsonSerializerOptionsExtensions
{
    extension(JsonSerializerOptions options)
    {
        public void ApplyDefaultOptions()
        {
            // Changes in this file should be applied on AppJsonContext.cs, IdentityJsonContext.cs and ServerJsonContext.cs as well,
            // so source generators can use the same options.

            options.AllowTrailingCommas = true;
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            foreach (var converter in AppJsonContext.Default.Options.Converters)
            {
                options.Converters.Add(converter);
            }

            options.TypeInfoResolverChain.AddRange([AppJsonContext.Default,
                IdentityJsonContext.Default,
                ServerJsonContext.Default]);
        }
    }
}
