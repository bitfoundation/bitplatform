using Microsoft.AspNetCore.Identity;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Server.Api.Features.Chatbot;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Server.Api.Features.Categories;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Multitenancy;

/// <summary>
/// A store of its own on the Sales deployment - a tenant, its admin, a category and one car the main store does not
/// sell - seen through every surface that is tenant scoped: the api, the sitemap, the home page and the chatbot's
/// product search over SignalR. The main store sells Mercedes-Benz, BMW, Ford, Nissan and Tesla; this one sells a BYD, so
/// an answer or a page that names the wrong store's cars is unmistakable.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebTenantIsolationTests : AppTestBase
{
    private const string password = "123456";

    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    public async Task ATenantsMember_Should_SeeOnlyItsOwnCatalogue_InTheApiThePagesAndTheChatbot()
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var marker = Guid.NewGuid().ToString("N")[..10];
        var email = $"e2e-{marker}@bitplatform.dev";
        var productName = $"BYD Seal {marker}";

        // A few of the main store's cars, which the new store's member must not be shown.
        var mainStoreCars = await dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.TenantId == TenantConfiguration.FallbackTenantId)
            .OrderBy(p => p.Name)
            .Select(p => p.Name!)
            .Take(3)
            .ToArrayAsync(TestContext.CancellationToken);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"e2e-{marker}", Title = $"E2E EV store {marker}" };

        // Registered before the writes, so a half-made store is taken down too.
        RegisterForCleanup(() => DeleteTenant(tenant.Id));

        await CreateStore(dbContext, tenant, email);

        // ---- The car, through the api: ProductController.Create is what computes the embedding the chatbot searches.
        // It is the Admin module's, so AdminPanel's api writes it - into the database Sales reads too. ----
        await using var adminApiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.AdminPanelApi);
        await adminApiClient.Services.GetRequiredService<AuthManager>().SignIn(new() { Email = email, Password = password, RememberMe = true }, TestContext.CancellationToken);

        var categoryId = await dbContext.Categories.IgnoreQueryFilters()
            .Where(c => c.TenantId == tenant.Id)
            .Select(c => c.Id)
            .SingleAsync(TestContext.CancellationToken);

        var created = await adminApiClient.Services.GetRequiredService<IProductController>().Create(new ProductDto
        {
            Id = Guid.CreateSequentialGuid(),
            Name = productName,
            Price = 41_990M,
            CategoryId = categoryId,
            DescriptionText = "A Chinese all-electric sports sedan by BYD with a long range Blade battery.",
            DescriptionHTML = "<p>A Chinese all-electric sports sedan by BYD with a long range Blade battery.</p>"
        }, TestContext.CancellationToken);

        // ---- API: the member's catalogue on Sales - whose ProductViewController it is - is the store's own ----
        await using var memberApiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.Sales);
        await memberApiClient.Services.GetRequiredService<AuthManager>().SignIn(new() { Email = email, Password = password, RememberMe = true }, TestContext.CancellationToken);

        var seenByMember = await memberApiClient.Services.GetRequiredService<IProductViewController>().Get(TestContext.CancellationToken);

        Assert.AreEqual(productName, seenByMember.Single().Name,
            $"The member of '{tenant.Name}' should be served that store's one product, got: {string.Join(", ", seenByMember.Select(p => p.Name))}.");

        // ---- Sitemap: anonymous, so the host's store - the main one - and nothing of the new one ----
        using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) })
        {
            var productsXml = await httpClient.GetStringAsync(new Uri(new Uri(DeployedApps.Sales), "products.xml"), TestContext.CancellationToken);

            Assert.Contains($"{PageUrls.Product}/", productsXml, "products.xml lists no product at all, so its silence about the new store proves nothing.");
            Assert.DoesNotContain($"{PageUrls.Product}/{created.ShortId}", productsXml,
                "products.xml is anonymous, so it is the host's store; it must not advertise another store's product.");
        }

        // ---- UI: the home page of the signed-in member ----
        // Straight to sign-in rather than through Home: the carousel's ProductView/Get is UserAgnostic with a five
        // minute max-age (See AppResponseCachePolicy), so opening Home anonymously first would put the host store's
        // answer in the browser cache, and the member would then be shown it from there, off the very same url.
        var page = Page;
        await page.GotoAsync(new Uri(new Uri(DeployedApps.Sales), PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitUntilInteractive(page);

        await SignIn(page, email, password);
        await page.GoToInApp(PageUrls.Home);

        await Expect(page.GetByText(productName, new() { Exact = true }).First).ToBeVisibleAsync();

        foreach (var mainStoreCar in mainStoreCars)
        {
            await Expect(page.GetByText(mainStoreCar, new() { Exact = true })).ToHaveCountAsync(0);
        }

        // ---- SignalR: the chatbot searches the member's store ----
        const string question = "I'm looking for a Chinese electric car, something like a BYD. Which cars do you have?";

        var panel = await AiChatPanel.Open(page);
        var answer = await panel.Ask(question);

        await AiAnswerJudge.AssertAnswer(question,
            $"""
            The assistant offers the car "{productName}" (a BYD) from its catalogue. It must not offer or recommend any
            Mercedes-Benz, BMW, Ford, Nissan or Tesla car - those belong to another store, and naming one means it
            searched the wrong catalogue. Saying it has no such car fails as well.
            """,
            answer, TestContext.CancellationToken);
    }

    /// <summary>
    /// The tenant, its admin (an accepted member holding t-admin in it), a category, and the system prompts
    /// TenantController.Create gives every tenant - without them the chatbot has no prompt to answer with.
    /// </summary>
    private async Task CreateStore(AppDbContext dbContext, Tenant tenant, string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email.Split('@')[0],
            NormalizedUserName = email.Split('@')[0].ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedOn = DateTimeOffset.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        // t-admin implies every tenant feature ProductController needs (See AppFeatures.GetRoleImpliedFeatures).
        var tenantAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = AppRoles.TenantAdmin,
            NormalizedName = AppRoles.TenantAdmin.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            TenantId = tenant.Id
        };

        await dbContext.Tenants.AddAsync(tenant, TestContext.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.CancellationToken);
        // Accepted, so signing in selects this tenant (See IdentityController.GetTenantId).
        await dbContext.TenantUsers.AddAsync(new TenantUser { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id, AcceptedOn = DateTimeOffset.UtcNow }, TestContext.CancellationToken);
        await dbContext.Roles.AddAsync(tenantAdminRole, TestContext.CancellationToken);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = tenantAdminRole.Id, TenantId = tenant.Id }, TestContext.CancellationToken);
        await dbContext.Categories.AddAsync(new Category { Id = Guid.NewGuid(), Name = "BYD", Color = "#D32F2F", TenantId = tenant.Id }, TestContext.CancellationToken);
        await dbContext.SystemPrompts.AddRangeAsync([
            new SystemPrompt { Id = Guid.NewGuid(), PromptKind = PromptKind.Support, Markdown = SystemPromptConfiguration.GetInitialSystemPromptMarkdown(), TenantId = tenant.Id },
            new SystemPrompt { Id = Guid.NewGuid(), PromptKind = PromptKind.AnalyzeProductImage, Markdown = SystemPromptConfiguration.GetAnalyzeProductImageSystemPromptMarkdown(), TenantId = tenant.Id }
        ], TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteTenant(Guid tenantId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var userIds = await db.TenantUsers.IgnoreQueryFilters().Where(tu => tu.TenantId == tenantId).Select(tu => tu.UserId).ToListAsync(CancellationToken.None);
        var roleIds = await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).Select(role => role.Id).ToListAsync(CancellationToken.None);

        await db.Products.IgnoreQueryFilters().Where(p => p.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Categories.IgnoreQueryFilters().Where(c => c.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.SystemPrompts.IgnoreQueryFilters().Where(prompt => prompt.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.UserRoles.IgnoreQueryFilters().Where(ur => userIds.Contains(ur.UserId) || roleIds.Contains(ur.RoleId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.RoleClaims.IgnoreQueryFilters().Where(rc => roleIds.Contains(rc.RoleId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.UserSessions.IgnoreQueryFilters().Where(session => userIds.Contains(session.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.TenantUsers.IgnoreQueryFilters().Where(tu => tu.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Users.IgnoreQueryFilters().Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync(CancellationToken.None);
        await db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tenantId).ExecuteDeleteAsync(CancellationToken.None);
    }
}
