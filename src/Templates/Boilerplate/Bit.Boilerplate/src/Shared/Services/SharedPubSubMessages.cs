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
    //#endif

    public const string SESSION_REVOKED = nameof(SESSION_REVOKED);
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

    /// <summary>
    /// While users are allowed to have multiple concurrent user sessions by **signing in** on multiple devices or browsers,  
    /// copying access and refresh tokens from one device / browser to another is prohibited.
    ///   
    /// This method uses a SignalR mechanism to check for potential misuse of access and refresh tokens across different app instances:  
    /// - The client calls a `Ping` method on the server.
    /// - The server sends a `PONG` to the specific user session.  
    /// - Because SignalR's connection id is the same is user session's Id (Thanks to AppHubConnectionHandler's implementation),
    /// - Only one user session will receive the `PONG` event.
    /// - If the validation event is not received within a 3-second timeout, it indicates that there's something wrong.
    ///   - The user might have illegally copied tokens to another device or browser.
    ///   - The user may have lost their SignalR connection to the server.
    ///   - It could also be another open tab in the same browser or another instance of the app.
    /// </summary>
    public const string PONG = nameof(PONG);
}
