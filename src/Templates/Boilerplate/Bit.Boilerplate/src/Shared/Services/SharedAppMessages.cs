//+:cnd:noEmit
namespace Boilerplate.Shared.Services;

/// <summary>
/// This class is located in the Shared project to define
/// shared app messages used for messaging between server and client through SignalR.
/// For client-only pub/sub messages, refer to the ClientAppMessages class in the Client/Core project.
/// </summary>
public partial class SharedAppMessages
{
    #region Server commands to client

    /// <summary>
    /// A publisher that sends this message announces that the subscriber should show a message to the user.
    /// </summary>
    public const string SHOW_MESSAGE = nameof(SHOW_MESSAGE);

    /// <summary>
    /// A publisher that sends this message lets the subscriber know that an exception has been thrown at the server side.
    /// </summary>
    public const string EXCEPTION_THROWN = nameof(EXCEPTION_THROWN);

    /// <summary>
    /// A publisher that sends this message announces that the subscriber should upload its diagnostic logger store to the server.
    /// 
    /// This allows super admins and support staff to retrieve all diagnostic logs for active user sessions.
    /// In contrast to production loggers (e.g., Sentry, AppInsights), which use a Warning level by default (except for specific categories at Information level) to reduce costs,
    /// the diagnostic logger defaults to Information level to capture all logs, stored solely in the client device's memory.
    /// Uploading these logs for display in the support staff's diagnostic modal log viewer aids in pinpointing the root cause of user issues during live troubleshooting.
    /// Another benefit of having this feature is in dev environment when you wanna see your Android, iOS logs on your desktop wide screen.
    /// </summary>
    public const string UPLOAD_DIAGNOSTIC_LOGGER_STORE = nameof(UPLOAD_DIAGNOSTIC_LOGGER_STORE);

    /// <summary>
    /// A publisher that sends this message announces that the subscriber should upload the last error that happened at client side to the server.
    /// </summary>
    public const string UPLOAD_LAST_ERROR = nameof(UPLOAD_LAST_ERROR);

    /// <summary>
    /// A publisher that sends this message announces that the subscriber should navigate to a specific page.
    /// </summary>
    public const string NAVIGATE_TO = nameof(NAVIGATE_TO);

    /// <summary>
    /// A publisher that sends this message announces that the subscriber should change the client's culture (language).
    /// </summary>
    public const string CHANGE_CULTURE = nameof(CHANGE_CULTURE);

    /// <summary>
    /// A publisher that sends this message announces that the subscriber should change the client's theme (light/dark).
    /// </summary>
    public const string CHANGE_THEME = nameof(CHANGE_THEME);

    /// <summary>
    /// A publisher that sends this message announces that the subscriber should clear all application files stored in the client device.
    /// </summary>
    public const string CLEAR_APP_FILES = nameof(CLEAR_APP_FILES);

    #endregion

    #region Server announcements to client

    //#if (module == "Admin")
    /// <summary>
    /// A publisher that sends this message announces that dashboard data has changed and subscribers should refresh their dashboard views.
    /// </summary>
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
    //#endif

    /// <summary>
    /// A publisher that sends this message announces that the subscriber's user's session has been revoked.
    /// </summary>
    public const string SESSION_REVOKED = nameof(SESSION_REVOKED);

    /// <summary>
    /// A publisher that sends this message announces that the subscriber's user's profile has been changed.
    /// </summary>
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);

    /// <summary>
    /// A publisher that sends this message announces that a specific background job has made some progress.
    /// This would let the client know about the progress of a job (Typically a long-running hangfire background job).
    /// </summary>
    public const string BACKGROUND_JOB_PROGRESS = nameof(BACKGROUND_JOB_PROGRESS);

    #endregion

    #region AI Chatbot messages

    /// <summary>
    /// This would let the client know that a chat bot encountered an error while processing the user's message.
    /// </summary>
    public const string MESSAGE_PROCESS_ERROR = nameof(MESSAGE_PROCESS_ERROR);

    /// <summary>
    /// This would let the client know that a chat bot successfully processed the user's message.
    /// </summary>
    public const string MESSAGE_PROCESS_SUCCESS = nameof(MESSAGE_PROCESS_SUCCESS);

    #endregion

    #region Client commands to server

    /// <summary>
    /// Using this message, the client asks the server to start a new chat session.
    /// </summary>
    public const string StartChat = nameof(StartChat);

    /// <summary>
    /// Using this message, the client notifies the server about a change in its authentication state.
    /// </summary>
    public const string ChangeAuthenticationState = nameof(ChangeAuthenticationState);

    /// <summary>
    /// Using this message, the client (typically support staff device), asks the server to get another user (typically a customer that needs support) session logs and send them back to the requested device.
    /// </summary>
    public const string GetUserSessionLogs = nameof(GetUserSessionLogs);

    #endregion

    public const string PUBLISH_MESSAGE = nameof(PUBLISH_MESSAGE);
}
