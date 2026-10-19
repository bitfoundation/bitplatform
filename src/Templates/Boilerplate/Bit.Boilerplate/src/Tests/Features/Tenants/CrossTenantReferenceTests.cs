using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Server.Api.Features.Products;
using Boilerplate.Server.Api.Features.Categories;

namespace Boilerplate.Tests.Features.Tenants;

[TestClass, TestCategory("IntegrationTest")]
public partial class CrossTenantReferenceTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AProductReferencingAnotherTenantsCategory_Should_BeRejectedByTheDatabase()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        var otherTenant = new Tenant { Id = Guid.CreateVersion7(), Name = $"cross-tenant-{Guid.NewGuid():N}" };
        var otherTenantCategory = new Category { Id = Guid.CreateVersion7(), Name = $"cross-tenant-{Guid.NewGuid():N}", TenantId = otherTenant.Id };

        await using (var scope = server.ApiApp.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.AddRangeAsync([otherTenant, otherTenantCategory], TestContext.CancellationToken);
            await dbContext.SaveChangesAsync(TestContext.CancellationToken);
        }

        try
        {
            await using var scope = server.ApiApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Products.AddAsync(new Product
            {
                Id = Guid.CreateVersion7(),
                ShortId = Random.Shared.Next(1_000_000, int.MaxValue),
                Name = $"cross-tenant-{Guid.NewGuid():N}",
                Price = 1,
                CategoryId = otherTenantCategory.Id,
                TenantId = TenantConfiguration.FallbackTenantId
            }, TestContext.CancellationToken);

            var exception = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(TestContext.CancellationToken));

            Assert.Contains("foreign key", exception.InnerException?.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await using var scope = server.ApiApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Products.IgnoreQueryFilters().Where(p => p.CategoryId == otherTenantCategory.Id).ExecuteDeleteAsync(CancellationToken.None);
            await dbContext.Categories.IgnoreQueryFilters().Where(c => c.Id == otherTenantCategory.Id).ExecuteDeleteAsync(CancellationToken.None);
            await dbContext.Tenants.Where(t => t.Id == otherTenant.Id).ExecuteDeleteAsync(CancellationToken.None);
        }
    }
}
