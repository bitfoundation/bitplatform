//+:cnd:noEmit
using Boilerplate.Client.Core;

namespace Boilerplate.Client.Maui;

public class ClientMauiSettings : ClientCoreSettings
{
    /// <summary>
    /// Specify the web app url in if API server and web app are hosted separately for proper link/url generation (e.g., email confirmation, social sign-in).
    /// </summary>
    public string? WebAppUrl { get; set; }
}
