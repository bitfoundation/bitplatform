//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Tenants;

public partial class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <summary>
    /// The default tenant that gets created by this entity type configuration and is used by all data seeds.
    /// It's also used as the fallback tenant by TenantProvider.
    /// </summary>
    public static readonly Guid FallbackTenantId = Guid.Parse("b1f71671-a1d6-4f97-abb9-d87d7b47d6e1");

    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasIndex(tenant => tenant.Name).IsUnique();

        builder.HasData(new Tenant
        {
            Id = FallbackTenantId,
            Name = "store",
            Title = "Store",
            IsActive = true,
            Version = 1
        });

        builder.HasUniqueIndexOnNullable(tenant => tenant.Domain);
    }
}
