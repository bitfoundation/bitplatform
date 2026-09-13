//+:cnd:noEmit
using System.Data.Common;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Shared.Features.Categories;

namespace Boilerplate.Tests.Features.Categories;

/// <summary>
/// <c>CategoryController.Delete</c> checks that the category is empty and then deletes it in two independent round
/// trips with no transaction. What made that a data-loss bug rather than a benign race is invisible from the
/// controller: nothing in the tree configures <c>OnDelete</c> for the <c>Category</c> -> <c>Product</c> relation, and
/// EF Core's convention for a <b>required</b> relationship is <c>DeleteBehavior.Cascade</c>. So a product that arrived
/// in the category between the two statements was silently destroyed by the database, with both requests reporting
/// success and nothing logged. <c>ExecuteDeleteAsync</c> bypasses the change tracker, so EF offered no safety net
/// either.
/// <para>
/// The relation is now <c>Restrict</c>, which is what these tests pin. The important one is the second: it deletes a
/// category that <i>does</i> hold products through the database directly, bypassing the controller's guard entirely -
/// which is exactly what the race achieves - and asserts the products survive. That assertion fails on the old model
/// and cannot be satisfied by any amount of application-level checking.
/// </para>
/// </summary>
/// <remarks>
/// <c>DoNotParallelize</c> for the same reason as <c>ProductImageLifecycleTests</c>: each of these tests creates a
/// product through the real endpoint, and without the ShortId sequence that value is a coarse clock reading
/// (See <c>Product.ShortId</c>), so concurrent creates collide on its unique index.
/// </remarks>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public partial class CategoryDeleteGuardTests
{
    // Seeded tenant-admin of the default (fallback) tenant; holds ProductCatalog_Manage. See UserConfiguration.
    private const string TenantAdminEmail = "store-admin@bitplatform.dev";
    private const string Password = "123456";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The guard's happy path, and the reason the friendly message still has to exist: a category holding a product
    /// must be refused with <c>CategoryNotEmpty</c> rather than a raw database error.
    /// </summary>
    [TestMethod]
    public async Task DeletingACategoryThatStillHasProducts_Should_BeRefused()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var categories = scope.ServiceProvider.GetRequiredService<ICategoryController>();
        var products = scope.ServiceProvider.GetRequiredService<IProductController>();

        var category = await categories.Create(NewCategoryDto(), TestContext.CancellationToken);
        var product = await products.Create(NewProductDto(category.Id), TestContext.CancellationToken);

        try
        {
            var refused = await Assert.ThrowsExactlyAsync<BadRequestException>(
                () => categories.Delete(category.Id, category.Version, TestContext.CancellationToken),
                "A non-empty category must be refused rather than deleted.");

            // The type alone would also be satisfied by an unrelated BadRequestException. Key is what survives the
            // wire - AppProblemDetails' implicit conversion re-attaches it client-side - and it is what pins this to
            // the guard's own localized message rather than to any 400 the endpoint happens to produce.
            Assert.AreEqual(nameof(AppStrings.CategoryNotEmpty), refused.Key,
                "The refusal must carry the localized CategoryNotEmpty message, not a generic bad request.");
        }
        finally
        {
            await products.Delete(product.Id, product.Version, CancellationToken.None);
            await categories.Delete(category.Id, category.Version, CancellationToken.None);
        }
    }

    /// <summary>
    /// The race, made deterministic. The controller's emptiness check is skipped entirely and the same
    /// <c>ExecuteDeleteAsync</c> it would have run is issued directly - which is precisely the state the two-statement
    /// window leaves the database in when a product lands in between. Under the old <c>Cascade</c> convention the
    /// delete succeeded and took the product with it; under <c>Restrict</c> the database refuses and the product
    /// survives.
    /// </summary>
    [TestMethod]
    public async Task DeletingACategoryBehindTheGuard_Should_NotDestroyItsProducts()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var categories = scope.ServiceProvider.GetRequiredService<ICategoryController>();
        var products = scope.ServiceProvider.GetRequiredService<IProductController>();

        var category = await categories.Create(NewCategoryDto(), TestContext.CancellationToken);
        var product = await products.Create(NewProductDto(category.Id), TestContext.CancellationToken);

        try
        {
            await using var dbScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();

            // IgnoreQueryFilters: this bare scope has no HttpContext, so the tenant-aware filter has no current tenant
            // to resolve. See TenantProvider.GetCurrentTenantId.
            // DbException, not Exception: the provider raises its own type for a foreign-key violation
            // (SqliteException, NpgsqlException, SqlException) and ExecuteDeleteAsync does not wrap it, so DbException
            // is the narrowest common base. Accepting any Exception would let a null reference or a tenant-filter
            // failure pass for the constraint this test is about.
            await Assert.ThrowsAsync<DbException>(
                () => dbContext.Categories
                                .IgnoreQueryFilters()
                                .Where(c => c.Id == category.Id)
                                .ExecuteDeleteAsync(TestContext.CancellationToken),
                "The foreign key must refuse this delete. If it succeeds, the relation is back to Cascade and the " +
                "product below is already gone.");

            Assert.IsTrue(
                await dbContext.Products.IgnoreQueryFilters().AnyAsync(p => p.Id == product.Id, TestContext.CancellationToken),
                "The product must still exist. This is the whole finding: an emptiness check the database does not " +
                "enforce turns an ordinary concurrent create into unrecoverable row loss.");
        }
        finally
        {
            await products.Delete(product.Id, product.Version, CancellationToken.None);
            await categories.Delete(category.Id, category.Version, CancellationToken.None);
        }
    }

    /// <summary>
    /// <c>Update</c>'s response is built by projecting from the database, not by mapping the tracked entity: the
    /// scalar <c>Map</c> reads <c>Category.Products.Count</c>, and <c>FindAsync</c> loads no collection navigation, so
    /// the mapped form reported 0 products for every category on a public API surface.
    /// </summary>
    [TestMethod]
    public async Task UpdatingACategory_Should_ReturnItsRealProductCount()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var categories = scope.ServiceProvider.GetRequiredService<ICategoryController>();
        var products = scope.ServiceProvider.GetRequiredService<IProductController>();

        var category = await categories.Create(NewCategoryDto(), TestContext.CancellationToken);
        var product = await products.Create(NewProductDto(category.Id), TestContext.CancellationToken);

        try
        {
            var toUpdate = await categories.Get(category.Id, TestContext.CancellationToken);
            Assert.AreEqual(1, toUpdate.ProductsCount, "The read path projects, so it was always right - this is the control.");

            toUpdate.Color = "#00FF00";
            var updated = await categories.Update(toUpdate, TestContext.CancellationToken);

            Assert.AreEqual("#00FF00", updated.Color);
            Assert.AreEqual(1, updated.ProductsCount,
                "Update must report the same count the read path does; 0 here means the response was mapped from an " +
                "entity whose Products navigation was never loaded.");
        }
        finally
        {
            await products.Delete(product.Id, product.Version, CancellationToken.None);
            var latest = await categories.Get(category.Id, CancellationToken.None);
            await categories.Delete(latest.Id, latest.Version, CancellationToken.None);
        }
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    private async Task SignIn(AsyncServiceScope scope)
    {
        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        var requiresTwoFactor = await authManager.SignIn(new()
        {
            Email = TenantAdminEmail,
            Password = Password
        }, TestContext.CancellationToken);

        Assert.IsFalse(requiresTwoFactor, $"'{TenantAdminEmail}' is not expected to have two factor authentication enabled.");
    }

    // Unique names keep these rows clear of the unique index on Name and of the other tests sharing one database.
    private static CategoryDto NewCategoryDto() => new()
    {
        Id = Guid.CreateSequentialGuid(),
        Name = $"delete-guard-{Guid.NewGuid():N}",
        Color = "#123456"
    };

    private static ProductDto NewProductDto(Guid categoryId) => new()
    {
        Id = Guid.CreateSequentialGuid(),
        Name = $"delete-guard-product-{Guid.NewGuid():N}",
        Price = 1_000M,
        CategoryId = categoryId
    };
}
