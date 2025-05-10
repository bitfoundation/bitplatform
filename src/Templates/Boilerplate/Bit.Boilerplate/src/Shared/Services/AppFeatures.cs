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

        /// <summary>
        /// Manage Roles.
        /// </summary>
        public const string ManageRoles = "1";

        /// <summary>
        /// Manage Users.
        /// </summary>
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
        /// <summary>
        /// View the admin panel's dashboard.
        /// </summary>
        public const string Dashboard = "5";
        /// <summary>
        /// Add/Modify/Delete products and categories.
        /// </summary>
        public const string ManageProductCatalog = "6";
    }

    public class Todo
    {
        /// <summary>
        /// Add/Modify/Delete todo items in /todo page.
        /// </summary>
        public const string ManageTodo = "7";
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
