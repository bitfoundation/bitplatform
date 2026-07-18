//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif
using Boilerplate.Server.Api.Features.Products;

namespace Boilerplate.Server.Api.Features.Categories;

public partial class Category
//#if (multitenant == true)
    : ITenantAware
//#endif
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; }

    public long Version { get; set; }

    //#if (multitenant == true)
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
    //#endif

    public IList<Product> Products { get; set; } = [];
}
