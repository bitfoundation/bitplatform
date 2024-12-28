//+:cnd:noEmit
using Boilerplate.Client.Core;

namespace Boilerplate.Client.Maui;

public class ClientMauiSettings : ClientCoreSettings
{
    /// <summary>
    /// When the maui app sends a request to the API server, and the API server and web app are hosted on different URLs,
    /// the origin of the generated links (e.g., email confirmation links) will depend on `WebAppUrl` value.
    /// </summary>
    public Uri? WebAppUrl { get; set; }
}
