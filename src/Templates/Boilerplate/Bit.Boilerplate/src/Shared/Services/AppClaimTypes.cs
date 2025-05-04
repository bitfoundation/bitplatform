namespace Boilerplate.Shared.Services;

/// <summary>
/// Static claim types for the application. Claim types are fixed, unlike dynamic users and roles.
/// Add new claim types (e.g., "print" for printing) as features are implemented.
/// Keep names short to minimize access token size, but ensure each claim type is unique.
/// </summary>
public class AppClaimTypes
{
    public class Management
    {
        /// <summary>
        /// Change AI Chatbot's system prompt.
        /// </summary>
        public const string ManageAI = "m-ai";

        /// <summary>
        /// Add/Modify/Delete users and roles.
        /// </summary>
        public const string ManageIdentity = "m-identity";

        /// <summary>
        /// <inheritdoc cref="SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE" />
        /// </summary>
        public const string ViewLogs = "r-logs";

        /// <summary>
        /// Manage background jobs using hangfire's dashboard.
        /// </summary>
        public const string ManageJobs = "m-jobs";
    }

    public class AdminPanel
    {
        /// <summary>
        /// View the admin panel's dashboard.
        /// </summary>
        public const string Dashboard = "dash";
        /// <summary>
        /// Add/Modify/Delete products and categories.
        /// </summary>
        public const string ManageProductCatalog = "m-pc";
    }

    public class Todo
    {
        /// <summary>
        /// Add/Modify/Delete todo items in /todo page.
        /// </summary>
        public const string ManageTodo = "m-todo";
    }

    /// <summary>
    /// These claims may not be added to RoleClaims/UserClaims tables.
    /// The system itself will assign these claims to the user based on <see cref="AuthPolicies"/> policies.
    /// </summary>
    public class System
    {
        public const string SESSION_ID = "s-id";

        /// <summary>
        /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
        /// </summary>
        public const string PRIVILEGED_SESSION = "p-s";

        /// <summary>
        /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS"/>
        /// </summary>
        public const string ELEVATED_SESSION = "e-s";
    }
}

public class AppBuiltinRoles
{
    /// <summary>
    /// This role has all the permissions (claims).
    /// </summary>
    public const string SuperAdmin = "s-admin";

    /// <summary>
    /// The role of the users who sign-up using the app.
    /// </summary>
    public const string BasicUser = "b-user";

    // Note: The rest of the roles can be dynamically created by the super admin users in the app.
}
