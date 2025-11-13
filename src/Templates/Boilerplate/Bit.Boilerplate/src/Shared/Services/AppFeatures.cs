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
        /// It can be anything (1.0.0, m-1.0, m-ai etc), but it has to be unique.
        /// The reason behind small feature values is that they're stored in jwt token, so in order to keep jwt token payload small, such a short-unique values has been assigned.
        /// </summary>
        public const string ManageAiPrompt = "1.0";

        public const string ManageRoles = "1.1";

        public const string ManageUsers = "1.2";
    }

    public class System
    {
        /// <summary>
        /// <inheritdoc cref="SharedPubSubMessages.UPLOAD_DIAGNOSTIC_LOGGER_STORE" />
        /// </summary>
        public const string ManageLogs = "2.0";

        /// <summary>
        /// Manage background jobs using hangfire's dashboard.
        /// </summary>
        public const string ManageJobs = "2.1";
    }

    public class AdminPanel
    {
        public const string Dashboard = "3.0";

        /// <summary>
        /// Add/Modify/Delete products and categories.
        /// </summary>
        public const string ManageProductCatalog = "3.1";
    }

    public class Todo
    {
        public const string ManageTodo = "4.0";
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
