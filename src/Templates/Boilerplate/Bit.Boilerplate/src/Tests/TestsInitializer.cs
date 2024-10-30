//+:cnd:noEmit
using Microsoft.EntityFrameworkCore;
using Boilerplate.Server.Api.Data;
//#if (database  == 'Sqlite')
using Microsoft.Data.Sqlite;
//#endif
//#if (advancedTests == true)
using Boilerplate.Tests.PageTests.PageModels.Identity;
using Boilerplate.Tests.Extensions;
using Boilerplate.Client.Web;
//#endif
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Tests;

[TestClass]
public partial class TestsInitializer
{
    //#if (advancedTests == true)
    public static string AuthenticationState { get; private set; } = null!;
    //#endif

    [AssemblyInitialize]
    public static async Task Initialize(TestContext _)
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(
        //#if (advancedTests == true)
        configureTestConfigurations: configuration =>
        {
            //Run assembly initialization test in BlazorWebAssembly mode to cache .wasm files
            configuration["WebAppRender:BlazorMode"] = BlazorWebAppMode.BlazorWebAssembly.ToString();
        }
        //#endif
        ).Start();

        await InitializeDatabase(testServer);

        //#if (advancedTests == true)
        await InitializeAuthenticationState(testServer);
        //#endif
    }

    //#if (database  == 'Sqlite')
    //SQLite database in in-memory mode only lives as long as at least one connection to it is open
    //This connection is required to keep the database alive during the test run.
    private static SqliteConnection connection = null!;
    //#endif
    private static async Task InitializeDatabase(AppTestServer testServer)
    {
        if (testServer.WebApp.Environment.IsDevelopment())
        {
            await using var scope = testServer.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //#if (database  == 'Sqlite')
            if (dbContext.Database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture))
            {
                connection = new SqliteConnection(dbContext.Database.GetConnectionString());
                await connection.OpenAsync();
            }
            //#endif
            await dbContext.Database.MigrateAsync();
        }
    }

    //#if (advancedTests == true)
    private static async Task InitializeAuthenticationState(AppTestServer testServer)
    {
        var playwrightPage = new PageTest();
        await playwrightPage.Setup();
        await playwrightPage.BrowserSetup();
        await playwrightPage.ContextSetup();
        await playwrightPage.PageSetup();

        await playwrightPage.Context.EnableBlazorWasmCaching();

        var signinPage = new SignInPage(playwrightPage.Page, testServer.WebAppServerAddress);

        Assertions.SetDefaultExpectTimeout(30_000); // Extended timeout for initial WebAssembly load and caching

        await signinPage.Open();
        await signinPage.AssertOpen();

        Assertions.SetDefaultExpectTimeout(5_000); // Standard timeout for subsequent tests

        var signedInPage = await signinPage.SignInWithEmail();
        await signedInPage.AssertSignInSuccess();

        var state = await playwrightPage.Page.Context.StorageStateAsync();
        if (string.IsNullOrEmpty(state))
            throw new InvalidOperationException("Authentication state is null or empty.");

        AuthenticationState = state.Replace(testServer.WebAppServerAddress.OriginalString.TrimEnd('/'), "[ServerAddress]");

        if (playwrightPage.Page.Context.Browser is IBrowser browser)
        {
            await browser.CloseAsync();
            await browser.DisposeAsync();
        }
    }
    //#endif
}
