namespace Boilerplate.Server.Api.Features.Tenants;

/// <summary>
/// Entities implementing this interface are tenant scoped:
/// - Reads are protected by a global query filter added in AppDbContext.OnModelCreating (Row level security).
/// - Creates must assign <see cref="TenantId"/> explicitly from User.GetTenantId() (See CategoryController.Create as an example).
/// </summary>
public interface ITenantAware
{
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
}
