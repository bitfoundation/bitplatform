//+:cnd:noEmit
namespace Boilerplate.Shared.Services;

/// <summary>
/// This class is located in the Shared project to defines
/// message keys used for pub/sub messaging between server and client through SignalR.
/// For client-only pub/sub messages, refer to the ClientPubSubMessages class in the Client/Core project.
/// </summary>
public partial class SharedPubSubMessages
{
    //#if (module == "Admin")
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
    //#endif

    public const string SESSION_REVOKED = nameof(SESSION_REVOKED);

    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
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
    /// When exception is thrown in server side.
    /// </summary>
    public const string EXCEPTION_THROWN = nameof(EXCEPTION_THROWN);
}

public static partial class SignalRMethods
{
    /// <summary>
    /// Allows super admins and support staff to retrieve all diagnostic logs for active user sessions.
    /// In contrast to production loggers (e.g., Sentry, AppInsights), which use a Warning level by default (except for specific categories at Information level) to reduce costs,
    /// the diagnostic logger defaults to Information level to capture all logs, stored solely in the client device's memory.
    /// Uploading these logs for display in the support staff's diagnostic modal log viewer aids in pinpointing the root cause of user issues during live troubleshooting.
    /// </summary>
    public const string UPLOAD_DIAGNOSTIC_LOGGER_STORE = nameof(UPLOAD_DIAGNOSTIC_LOGGER_STORE);
}
