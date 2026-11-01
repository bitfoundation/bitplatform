namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A sample account a view can be signed in as, with one of the app's roles.</summary>
public sealed record WorkbenchDemoUser(string NameKey, string Email, string Role)
{
    /// <summary>The resource key of the role's display name.</summary>
    public string RoleKey => Role switch
    {
        AppRoles.GlobalAdmin => nameof(WorkbenchStrings.RoleGlobalAdmin),
        AppRoles.Demo => nameof(WorkbenchStrings.RoleDemo),
        _ => nameof(WorkbenchStrings.RoleTenantAdmin)
    };
}
