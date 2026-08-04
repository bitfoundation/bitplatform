namespace Boilerplate.Server.Api.Features.Tenants;

/// <summary>
/// Entities implementing this interface are tenant scoped:
/// - Reads are protected by a global query filter added in AppDbContext.OnModelCreating (Row level security).
/// - Creates get <see cref="TenantId"/> stamped by AppDbContext.OnSavingChanges when it is still default, resolved
///   through TenantProvider - which throws when there is no HttpContext (a background job) and falls back to the
///   default tenant for an unrecognised host. Assign it yourself when the row belongs to a tenant other than the
///   current one, or when you want a write with no positively identified tenant to fail rather than land in the
///   default one (See TenantController.Create as an example).
/// </summary>
public interface ITenantAware
{
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
}
