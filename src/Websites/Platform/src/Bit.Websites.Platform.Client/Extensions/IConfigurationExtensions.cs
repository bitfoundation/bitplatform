namespace Microsoft.Extensions.Configuration;
public static class IConfigurationExtensions
{
    public static string GetApiServerAddress(this IConfiguration configuration)
    {
        var apiServerAddress = configuration.GetValue("ApiServerAddress", defaultValue: "/")!;

        return Uri.TryCreate(apiServerAddress, UriKind.RelativeOrAbsolute, out _) ? apiServerAddress : throw new InvalidOperationException($"Api server address {apiServerAddress} is invalid");
    }
}
