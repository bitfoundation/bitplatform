using Microsoft.AspNetCore.Identity;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Todo;

/// <summary>
/// <c>OfflineTodoTests</c>' scenario (Boilerplate.Tests) on the real thing: the offline todo demo is Blazor WebAssembly
/// standalone, published in Release with EF Core's compiled model for its in-browser database and bswup's full offline
/// mode - none of which a test server running the source tree exercises. A todo item added while offline has to live in
/// the browser's database until the app is back online, and then reach the server's.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebOfflineTodoSyncTests : AppTestBase
{
    private const string password = "123456";

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// A user of its own: store-user is signed in by other tests too, and the todo page needs a privileged session,
    /// which a user holding three already cannot get.
    /// </summary>
    [TestMethod]
    public async Task ATodoItemAddedOffline_Should_ReachTheServer_OnceTheAppIsBackOnline()
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var marker = Guid.NewGuid().ToString("N")[..10];
        var email = $"e2e-{marker}@bitplatform.dev";
        var offlineTitle = $"added offline {marker}";
        var onlineTitle = $"added online {marker}";

        var userId = await CreateTodoUser(dbContext, email);

        var page = await OpenApp(App.TodoOffline);
        await WaitUntilInteractive(page);

        await SignIn(page, email, password);
        await page.GoToInApp(PageUrls.OfflineTodo);

        await Expect(page).ToHaveTitleAsync(AppStrings.OfflineTodoTitle);
        await Expect(page.GetByPlaceholder(AppStrings.TodoAddPlaceholder)).ToBeVisibleAsync();
        // Full offline mode: every asset has to be in the service worker's cache before the network goes away.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // ---- Offline: the item lives only in the browser's database ----
        await Context.SetOfflineAsync(offline: true);
        await AddTodoItem(page, offlineTitle);

        Assert.IsEmpty(await TitlesOnTheServer(dbContext, userId),
            "The item added while offline is already on the server, so nothing was ever offline.");

        // ---- Back online: the reload reconnects the app, and the next change pushes everything pending ----
        await Context.SetOfflineAsync(offline: false);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(page.GetByPlaceholder(AppStrings.TodoAddPlaceholder)).ToBeVisibleAsync();
        await Expect(page.GetByText(offlineTitle)).ToBeVisibleAsync();

        await AddTodoItem(page, onlineTitle);

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        string[] synced;

        while ((synced = await TitlesOnTheServer(dbContext, userId)).Length < 2 && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);
        }

        Assert.Contains(offlineTitle, synced, "The item added while offline never reached the server's database.");
        Assert.Contains(onlineTitle, synced, "The item added once back online never reached the server's database.");
    }

    private async Task AddTodoItem(IPage page, string title)
    {
        await page.GetByPlaceholder(AppStrings.TodoAddPlaceholder).FillEnsuringStable(title);
        // Disabled until the debounced binding has the title; ClickAsync waits for it to be enabled.
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Add, Exact = true }).ClickAsync();

        await Expect(page.GetByText(title)).ToBeVisibleAsync();
    }

    private async Task<string[]> TitlesOnTheServer(AppDbContext dbContext, Guid userId)
    {
        return await dbContext.TodoItems.IgnoreQueryFilters().AsNoTracking()
            .Where(todoItem => todoItem.UserId == userId)
            .Select(todoItem => todoItem.Title!)
            .ToArrayAsync(TestContext.CancellationToken);
    }

    /// <summary>A confirmed user in the store's demo user-group, which carries Todo_Manage_Self.</summary>
    private async Task<Guid> CreateTodoUser(AppDbContext dbContext, string email)
    {
        var demoRoleId = await dbContext.Roles.IgnoreQueryFilters()
            .Where(role => role.Name == AppRoles.Demo && role.TenantId == TenantConfiguration.FallbackTenantId)
            .Select(role => role.Id)
            .SingleAsync(TestContext.CancellationToken);

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

        // Registered before the write, so a half-made user is taken down too.
        RegisterForCleanup(() => DeleteUser(user.Id));

        await dbContext.Users.AddAsync(user, TestContext.CancellationToken);
        await dbContext.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = demoRoleId, TenantId = TenantConfiguration.FallbackTenantId }, TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return user.Id;
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteUser(Guid userId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await db.TodoItems.IgnoreQueryFilters().Where(todoItem => todoItem.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.UserSessions.IgnoreQueryFilters().Where(session => session.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.UserRoles.IgnoreQueryFilters().Where(userRole => userRole.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Users.IgnoreQueryFilters().Where(user => user.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
    }
}
