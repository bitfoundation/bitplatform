//+:cnd:noEmit
namespace Boilerplate.Shared.Services;

/// <summary>
/// This class is located in the Shared project to defines
/// message keys used for pub/sub messaging between server and client through SignalR.
/// For client-only pub/sub messages, refer to the ClientPubSubMessages class in the Client/Core project.
/// </summary>
public static partial class SharedPubSubMessages
{
    //#if (sample == "Admin")
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
    //#else
    // To see examples of this class, checkout the admin panel sample.
    //#endif
}

public static partial class SignalREvents
{
    /// <summary>
    /// Sends a message to the clients that will published through PubSubService.
    /// </summary>
    public const string PUBLISH_MESSAGE = nameof(PUBLISH_MESSAGE);

    /// <summary>
    /// Shows message at client side.
    /// </summary>
    public const string SHOW_MESSAGE = nameof(SHOW_MESSAGE);

    public const string SESSION_REVOKED = nameof(SESSION_REVOKED);
}
