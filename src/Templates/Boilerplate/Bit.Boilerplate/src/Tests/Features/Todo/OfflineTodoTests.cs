using Microsoft.EntityFrameworkCore;
using Boilerplate.Tests.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Features.Todo;

[TestClass, TestCategory("UITest")]
public partial class OfflineTodoTests : PageTest
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
        Page.SetDefaultTimeout((float)TimeSpan.FromSeconds(90).TotalMilliseconds);

        var firstTodoTitle = Guid.NewGuid().ToString();
        var secondTodoTitle = Guid.NewGuid().ToString();

        await using var server = new AppTestServer();
        var serverAddress = server.WebAppServerAddress;

        // In Blazor WebAssembly mode the client calls the absolute ServerAddress from its configuration, not the origin
        // it was served from. Push our (random free port) server address into the WebAssembly app before it boots so it
        // talks to this test server instead of the address baked into Client.Core/appsettings.json (see the advancedTests
        // block in Client.Web/Program.cs).
        await SetBlazorWebAssemblyServerAddress(serverAddress);

        await server.Build(configureTestConfigurations: RunInBlazorWebAssemblyMode).Start(TestContext.CancellationToken);

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

        var syncedTitles = await GetTodoItemTitlesFromServerDatabase(server, [firstTodoTitle, secondTodoTitle], TimeSpan.FromSeconds(30));

        CollectionAssert.Contains(syncedTitles, firstTodoTitle, "The todo item added while offline was not synced to the server database.");
        CollectionAssert.Contains(syncedTitles, secondTodoTitle, "The todo item added while online was not synced to the server database.");
    }

    private static void RunInBlazorWebAssemblyMode(ConfigurationManager configuration)
    {
        configuration["WebAppRender:BlazorMode"] = nameof(BlazorWebAppMode.BlazorWebAssembly);
    }

    /// <summary>
    /// Passes the server address to the Blazor WebAssembly app through a <c>startupParams</c> JS function that
    /// Client.Web/Program.cs reads on startup (see its advancedTests block), overriding the app's configured
    /// ServerAddress so the browser app talks to our test server rather than a hard-coded address.
    /// More info: https://stackoverflow.com/questions/60831359/how-are-string-args-passed-to-program-main-in-a-blazor-webassembly-app
    /// </summary>
    private async Task SetBlazorWebAssemblyServerAddress(Uri serverAddress)
    {
        await Context.AddInitScriptAsync($"window.startupParams = function() {{ return [ 'ServerAddress={serverAddress}' ]; }};");
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

    private async Task<List<string?>> GetTodoItemTitlesFromServerDatabase(AppTestServer server, string[] titles, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

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

    public override BrowserNewContextOptions ContextOptions() => base.ContextOptions().EnableVideoRecording(TestContext);

    [TestCleanup]
    public async ValueTask Cleanup() => await Context.FinalizeVideoRecording(TestContext);
}
