//-:cnd:noEmit
namespace Microsoft.Extensions.Configuration;
public static class IConfigurationExtensions
{
    public static string GetServerAddress(this IConfiguration configuration)
    {
        var serverAddress = configuration.GetValue("ServerAddress", defaultValue: "/")!;

        if (BuildConfiguration.IsDebug() &&
            serverAddress.Contains("localhost", StringComparison.InvariantCultureIgnoreCase) &&
            OperatingSystem.IsAndroid())
        {
            const string androidEmulatorDevMachineIP = "10.0.2.2"; // Special alias to your host loopback interface in Android Emulators (127.0.0.1 on your development machine)

            serverAddress = serverAddress.Replace("localhost", androidEmulatorDevMachineIP, StringComparison.InvariantCultureIgnoreCase);
        }

        return Uri.TryCreate(serverAddress, UriKind.RelativeOrAbsolute, out _)
            ? serverAddress.TrimEnd('/')
            : throw new InvalidOperationException($"Api server address {serverAddress} is invalid");
    }
}
