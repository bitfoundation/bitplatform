namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddApiConfigurations(this IConfigurationBuilder builder, string env)
    {
        builder.AddSharedConfigurations();

        var appsettingsJsonFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var appsettingsEnvJsonFilePath = Path.Combine(AppContext.BaseDirectory, $"appsettings.{env}.json");

        builder.AddJsonFile(appsettingsJsonFilePath, optional: false, reloadOnChange: true);
        builder.AddJsonFile(appsettingsEnvJsonFilePath, optional: true, reloadOnChange: true);
    }
}
