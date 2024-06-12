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
            const string androidEmulatorDevMachineIP = "10.0.2.2"; // Special alias to your host loopback interface in Android Emulators (127.0.0.1 on your development machine)

            apiServerAddress = apiServerAddress.Replace("localhost", androidEmulatorDevMachineIP, StringComparison.InvariantCultureIgnoreCase);
        }

        return Uri.TryCreate(apiServerAddress, UriKind.RelativeOrAbsolute, out _)
            ? apiServerAddress.TrimEnd('/')
            : throw new InvalidOperationException($"Api server address {apiServerAddress} is invalid");
    }
}
