//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Categories;

public partial class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        //#if (multitenant == true)
        // The category name must be unique within the tenant, not globally.
        builder.HasIndex(p => new { p.TenantId, p.Name }).IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenant != true)
        builder.HasIndex(p => p.Name).IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        var defaultVersion = 1;
        builder.HasData(
            new()
            {
                Id = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"),
                Name = "Ford",
                Color = "#FFCD56",
                Version = defaultVersion,
                //#if (multitenant == true)
                TenantId = TenantConfiguration.FallbackTenantId,
                //#endif
            },
            new()
            {
                Id = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"),
                Name = "Nissan",
                Color = "#FF6384",
                Version = defaultVersion,
                //#if (multitenant == true)
                TenantId = TenantConfiguration.FallbackTenantId,
                //#endif
            },
            new()
            {
                Id = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"),
                Name = "Benz",
                Color = "#4BC0C0",
                Version = defaultVersion,
                //#if (multitenant == true)
                TenantId = TenantConfiguration.FallbackTenantId,
                //#endif
            },
            new()
            {
                Id = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"),
                Name = "BMW",
                Color = "#FF9124",
                Version = defaultVersion,
                //#if (multitenant == true)
                TenantId = TenantConfiguration.FallbackTenantId,
                //#endif
            },
            new()
            {
                Id = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"),
                Name = "Tesla",
                Color = "#2B88D8",
                Version = defaultVersion,
                //#if (multitenant == true)
                TenantId = TenantConfiguration.FallbackTenantId,
                //#endif
            });
    }
}

