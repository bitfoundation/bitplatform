//-:cnd:noEmit
namespace Microsoft.Extensions.Configuration;
public static class IConfigurationExtensions
{
    public static string GetApiServerAddress(this IConfiguration configuration)
    {
        var apiServerAddress = configuration.GetValue("ApiServerAddress", defaultValue: "/")!;

        if (BuildConfiguration.IsDebug() && 
            apiServerAddress.Contains("localhost", StringComparison.InvariantCultureIgnoreCase) &&
            OperatingSystem.IsAndroid())
        {
            apiServerAddress = apiServerAddress.Replace("localhost", "10.0.2.2", StringComparison.InvariantCultureIgnoreCase);
        }

        return Uri.TryCreate(apiServerAddress, UriKind.RelativeOrAbsolute, out _)
            ? apiServerAddress.TrimEnd('/')
            : throw new InvalidOperationException($"Api server address {apiServerAddress} is invalid");
    }
}
