namespace Boilerplate.Server.Api.Features.Tenants;

/// <summary>
/// SECURITY: <c>Tenant.Name</c> is the sub domain resolution key (See TenantProvider), and creating a tenant is
/// self-service. Uniqueness alone does not protect the deployment's own host label, because no tenant owns it:
/// a tenant named after it captures every anonymous request to the primary host, which also decides which
/// tenant's Demo role new sign-ups are granted (See UserManagerExtensions.CreateUserWithDemoRole).
/// </summary>
public static class ReservedTenantNames
{
    /// <summary>
    /// Labels that must never become a tenant name. Extend this for your own deployment's sub domains.
    /// </summary>
    public static readonly string[] Names =
    [
        "www", "api", "app", "admin", "adminpanel", "mail", "smtp", "imap", "ftp",
        "static", "assets", "cdn", "media", "files", "img", "images",
        "localhost", "dev", "test", "qa", "staging", "acc", "dr", "status", "docs", "blog", "support", "help",
        "auth", "login", "signin", "account", "billing", "dashboard", "portal"
    ];

    /// <summary>
    /// True when <paramref name="name"/> may not be used as a tenant name, either because it is a reserved
    /// label or because it is the first label of a host the deployment itself answers on.
    /// </summary>
    public static bool IsReserved(string? name, params string?[] deploymentHosts)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false; // Absent / blank names are the [Required] attribute's business, not ours.

        name = name.Trim();

        if (Names.Contains(name, StringComparer.OrdinalIgnoreCase))
            return true;

        foreach (var host in deploymentHosts)
        {
            if (string.IsNullOrWhiteSpace(host))
                continue;

            // Mirror TenantProvider exactly: it only resolves a tenant from the sub domain when the host has
            // more than two labels (`host.Split('.') is { Length: > 2 }` then `IdsByName[hostSegments[0]]`),
            // so an apex host such as example.com reserves nothing.
            if (host.Split('.') is { Length: > 2 } labels
                && string.Equals(name, labels[0], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
