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

    /// <summary>
    /// True when <paramref name="domain"/> may not be used as a tenant's custom domain, because it is a host the
    /// deployment itself answers on, or sits underneath one.
    /// </summary>
    public static bool IsReservedDomain(string? domain, params string?[] deploymentHosts)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false; // A blank custom domain simply means "resolve me by sub domain".

        domain = domain.Trim();

        foreach (var zone in deploymentHosts.SelectMany(GetReservedZones))
        {
            if (string.Equals(domain, zone, StringComparison.OrdinalIgnoreCase)
                || domain.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The zones a deployment host reserves. The host itself, plus its parent when it carries a sub domain label,
    /// because the request this is validated against may well arrive on a tenant's own sub domain
    /// (<c>acme.myapp.com</c>) - taking that host at face value would leave <c>myapp.com</c> and every sibling
    /// tenant's host claimable. A <c>*</c> wildcard from a TrustedOrigins entry reserves its whole zone.
    /// </summary>
    private static IEnumerable<string> GetReservedZones(string? deploymentHost)
    {
        if (string.IsNullOrWhiteSpace(deploymentHost))
            yield break;

        var host = deploymentHost.Trim().TrimEnd('.');

        if (host.StartsWith("*.", StringComparison.Ordinal))
        {
            yield return host[2..];
            yield break;
        }

        if (host.Contains('*'))
            yield break; // A mid-label wildcard (tenant-*.myapp.com) describes no single zone.

        yield return host;

        if (host.Split('.') is { Length: > 2 } labels)
            yield return string.Join('.', labels[1..]);
    }
}
