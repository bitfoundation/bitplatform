using Microsoft.EntityFrameworkCore;
using Boilerplate.Tests.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Features.Todo;

[TestClass, TestCategory("UITest")]
public partial class OfflineTodoTests : AppPageTest
{
    /// <summary>
    /// Verifies the offline-first sync flow of <c>OfflineTodoPage</c> while running the app in Blazor WebAssembly mode:
    /// <list type="number">
    /// <item>Sign-in with the default credentials and open the offline todo page.</item>
    /// <item>Take the browser offline and add a todo item whose title is a random Guid. It is stored only in the client-side offline database.</item>
    /// <item>Bring the browser back online and add another todo item whose title is a second random Guid. Being online again, the client pushes every pending change to the server.</item>
    /// <item>Resolve the server's <see cref="AppDbContext"/> and assert both random Guids were synced into the database.</item>
    /// </list>
    /// The whole scenario only makes sense in Blazor WebAssembly mode, because in Blazor Server mode going offline tears down the client's circuit, so the offline changes could not be made in the first place.
    /// </summary>
    [TestMethod]
    public async Task OfflineTodo_Should_SyncPendingChanges_WhenServerIsBackOnline()
    {


        var firstTodoTitle = Guid.NewGuid().ToString();
        var secondTodoTitle = Guid.NewGuid().ToString();

        await using var server = new AppTestServer(Context);
        var serverAddress = server.WebAppServerAddress;

        await server.Build(configureTestConfigurations: configuration => configuration["WebAppRender:BlazorMode"] = nameof(BlazorWebAppMode.BlazorWebAssembly))
            .Start(TestContext.CancellationToken);

        await SignIn(serverAddress);
        await GoToOfflineTodoPage(serverAddress);

        // Take the browser offline, then add a todo item; it lives only in the client-side offline database.
        await Context.SetOfflineAsync(offline: true);
        await AddTodoItem(firstTodoTitle);

        // Back online: reloading reconnects the app, then adding another item pushes every pending change to the server.
        await Context.SetOfflineAsync(offline: false);
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.GetByPlaceholder(AppStrings.TodoAddPlaceholder)).ToBeVisibleAsync();
        await AddTodoItem(secondTodoTitle);

        var syncedTitles = await GetTodoItemTitlesFromServerDatabase(server, [firstTodoTitle, secondTodoTitle]);

        Assert.Contains(firstTodoTitle, syncedTitles, "The todo item added while offline was not synced to the server database.");
        Assert.Contains(secondTodoTitle, syncedTitles, "The todo item added while online was not synced to the server database.");
    }

    private async Task SignIn(Uri serverAddress)
    {
        await Page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(TestData.DefaultTestEmail);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(TestData.DefaultTestPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(serverAddress.ToString());
    }

    private async Task GoToOfflineTodoPage(Uri serverAddress)
    {
        await Page.GotoAsync(new Uri(serverAddress, PageUrls.OfflineTodo).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Expect(Page).ToHaveTitleAsync(AppStrings.OfflineTodoTitle);
        await Expect(Page.GetByPlaceholder(AppStrings.TodoAddPlaceholder)).ToBeVisibleAsync();
    }

    private async Task AddTodoItem(string title)
    {
        await Page.GetByPlaceholder(AppStrings.TodoAddPlaceholder).FillAsync(title);
        // The Add button is disabled until the debounced title binding kicks in; ClickAsync waits for it to become enabled.
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Add, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(title)).ToBeVisibleAsync();
    }

    private async Task<List<string?>> GetTodoItemTitlesFromServerDatabase(AppTestServer server, string[] titles)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

        while (true)
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var syncedTitles = await dbContext.TodoItems.AsNoTracking()
                .Where(todoItem => titles.Contains(todoItem.Title))
                .Select(todoItem => todoItem.Title)
                .ToListAsync(TestContext.CancellationToken);

            if (syncedTitles.Count >= titles.Length || DateTimeOffset.UtcNow >= deadline)
                return syncedTitles;

            await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);
        }
    }
}
