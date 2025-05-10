namespace Boilerplate.Shared.Services;

/// <summary>
/// Values for the <see cref="AppClaimTypes.FEATURES"/>
/// These features will be implemented as a policy. If the user has the specified value in the <see cref="AppClaimTypes.FEATURES"/> claim,
/// the policy will be fulfilled, granting the user access to the resource <see cref="ISharedServiceCollectionExtensions.ConfigureAuthorizationCore"/>
/// </summary>
public class AppFeatures
{
    public class Management
    {
        /// <summary>
        /// Change AI Chatbot's system prompt.
        /// </summary>
        public const string ManageAiPrompt = "0";

        public const string ManageRoles = "1";

        public const string ManageUsers = "2";
    }

    public class System
    {
        /// <summary>
        /// <inheritdoc cref="SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE" />
        /// </summary>
        public const string ManageLogs = "3";

        /// <summary>
        /// Manage background jobs using hangfire's dashboard.
        /// </summary>
        public const string ManageJobs = "4";
    }

    public class AdminPanel
    {
        public const string Dashboard = "5";

        /// <summary>
        /// Add/Modify/Delete products and categories.
        /// </summary>
        public const string ManageProductCatalog = "6";
    }

    public class Todo
    {
        public const string ManageTodo = "7";
    }

    public class Extra
    {
        /// <summary>
        /// This feature is accessible to all users, including anonymous ones,
        /// so no server-side authorization is required.
        /// They are listed here solely to allow super admins to manage them,
        /// such as enabling or disabling these features.
        /// To restrict these features to authorized users only,
        /// apply authorization attributes to their respective controllers.
        /// </summary>
        public const string ViewAboutApp = "8";
    }

    public class Communication
    {
        /// <summary>
        /// Chat with the AI Chatbot
        /// <inheritdoc cref="Extra.ViewAboutApp" />
        /// </summary>
        public const string Chatbot = "9";

        /// <summary>
        /// Ability to use the SignalR channel for real-time communication.
        /// <inheritdoc cref="Extra.ViewAboutApp" />
        /// </summary>
        public const string SignalR = "10";

        /// <summary>
        /// Ability to receive push notifications.
        /// <inheritdoc cref="Extra.ViewAboutApp" />
        /// </summary>
        public const string PushNotification = "11";
    }

    public class Sales
    {
        /// <summary>
        /// <inheritdoc cref="Extra.ViewAboutApp" />
        /// </summary>
        public const string ProductView = "12";
    }


    public static (string Name, string Value, Type Group)[] GetAll() => GetSuperAdminFeatures();

    private static (string Name, string Value, Type Group)[]? allFeatures;
    public static (string Name, string Value, Type Group)[] GetSuperAdminFeatures()
    {
        return allFeatures ??= [.. typeof(AppFeatures)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields())
            .Select(t => (t.Name, t.GetRawConstantValue()!.ToString()!, t.DeclaringType!))];
    }
}
