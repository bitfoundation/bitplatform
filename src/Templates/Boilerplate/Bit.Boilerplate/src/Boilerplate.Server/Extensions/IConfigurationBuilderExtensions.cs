namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddApiConfigurations(this IConfigurationBuilder builder, string env)
    {
        builder.AddSharedConfigurations();

        var appsettingsJsonFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var appsettingsEnvJsonFilePath = Path.Combine(AppContext.BaseDirectory, $"appsettings.{env}.json");

        if (File.Exists(appsettingsJsonFilePath))
            builder.AddJsonFile(appsettingsJsonFilePath, optional: false, reloadOnChange: true);

        if (File.Exists(appsettingsEnvJsonFilePath))
            builder.AddJsonFile(appsettingsEnvJsonFilePath, optional: false, reloadOnChange: true);
    }
}
