//-:cnd:noEmit
namespace Microsoft.Extensions.Configuration;
public static class IConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string name)
    {
        return (configuration ?? throw new ArgumentNullException(nameof(configuration)))
            .GetValue<T>(name ?? throw new ArgumentNullException(nameof(name))) ?? throw new InvalidOperationException($"{name} config could not be found");
    }

    private static Uri? serverAddressUri;

    public static Uri GetServerAddress(this IConfiguration configuration, Uri? baseUrl = null)
    {
        if (serverAddressUri is not null)
            return serverAddressUri;

        var serverAddress = configuration.GetRequiredValue<string>("ServerAddress");

        if (AppEnvironment.IsDev() &&
            serverAddress.Contains("localhost", StringComparison.InvariantCultureIgnoreCase) &&
            OperatingSystem.IsAndroid())
        {
            const string androidEmulatorDevMachineIP = "10.0.2.2"; // Special alias to your host loopback interface in Android Emulators (127.0.0.1 on your development machine)

            serverAddress = serverAddress.Replace("localhost", androidEmulatorDevMachineIP, StringComparison.InvariantCultureIgnoreCase);
        }

        if (Uri.TryCreate(serverAddress, UriKind.RelativeOrAbsolute, out var uri) is false)
            throw new InvalidOperationException($"Api server address {serverAddress} is invalid");

        if (uri.IsAbsoluteUri is false)
        {
            uri = new Uri(baseUrl!, uri);
        }

        return serverAddressUri = uri;
    }
}
