namespace Microsoft.Extensions.Configuration;

public static class IConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string key)
    {
        var connectionString = configuration.GetConnectionString(key);
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException($"Connection string '{key}' is not found.");
        return connectionString;
    }
}
