namespace Boilerplate.Shared.Services;

/// <summary>
/// Values for the <see cref="AppClaimTypes.PERMISSIONS"/>
/// These permissions will be implemented as a policy. If the user has the specified value in the <see cref="AppClaimTypes.PERMISSIONS"/> claim,
/// the policy will be fulfilled, granting the user access to the resource <see cref="ISharedServiceCollectionExtensions.ConfigureAuthorizationCore"/>
/// </summary>
public class AppPermissions
{
    public class Management
    {
        /// <summary>
        /// Change AI Chatbot's system prompt.
        /// </summary>
        public const string ManageAI = "0";

        /// <summary>
        /// Add/Modify/Delete users and roles.
        /// </summary>
        public const string ManageIdentity = "1";

        /// <summary>
        /// <inheritdoc cref="SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE" />
        /// </summary>
        public const string ViewLogs = "2";

        /// <summary>
        /// Manage background jobs using hangfire's dashboard.
        /// </summary>
        public const string ManageJobs = "3";
    }

    public class AdminPanel
    {
        /// <summary>
        /// View the admin panel's dashboard.
        /// </summary>
        [Display(Name = nameof(AppStrings.PhoneNumber))]
        public const string Dashboard = "4";
        /// <summary>
        /// Add/Modify/Delete products and categories.
        /// </summary>
        public const string ManageProductCatalog = "5";
    }

    public class Todo
    {
        /// <summary>
        /// Add/Modify/Delete todo items in /todo page.
        /// </summary>
        public const string ManageTodo = "6";
    }

    public static (string permissionKey, string value, Type group)[] GetAll() => GetSuperAdminPermissions();

    public static (string permissionKey, string value, Type group)[] GetSuperAdminPermissions()
    {
        return [.. typeof(AppPermissions)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields())
            .Select(t => (t.Name, t.GetRawConstantValue()!.ToString()!, t.DeclaringType!))];
    }

    public static (string permissionKey, string value, Type group)[] GetBasicUserPermissions()
    {
        return [.. GetSuperAdminPermissions()
            .Where(p => p.group != typeof(Management))];
    }
}
