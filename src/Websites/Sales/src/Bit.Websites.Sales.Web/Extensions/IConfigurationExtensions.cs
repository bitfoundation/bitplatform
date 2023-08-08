namespace Microsoft.Extensions.Configuration;
public static class IConfigurationExtensions
{
    public static string GetApiServerAddress(this IConfiguration configuration)
    {
        return configuration.GetValue<string?>("ApiServerAddress") ?? throw new InvalidOperationException("Could not find ApiServerAddress config");
    }
}
