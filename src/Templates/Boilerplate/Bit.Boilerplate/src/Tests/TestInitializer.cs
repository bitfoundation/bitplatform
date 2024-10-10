﻿using Boilerplate.Server.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Tests;

[TestClass]
public partial class TestInitializer
{
    public static string AuthenticationState { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task Initialize(TestContext _)
    {
        await using var testServer = new AppTestServer();

        await testServer.Build().Start();

        await InitializeDatabase(testServer);

        await InitializeAuthToken(testServer);
    }

    private static async Task InitializeDatabase(AppTestServer testServer)
    {
        if (AppEnvironment.IsDev())
        {
            await using var scope = testServer.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }

    private static async Task InitializeAuthToken(AppTestServer testServer)
    {
        var playwrightPage = new PageTest();
        await playwrightPage.Setup();
        await playwrightPage.BrowserSetup();
        await playwrightPage.ContextSetup();
        await playwrightPage.PageSetup();

        var signinPage = new SignInPage(playwrightPage.Page, testServer.WebAppServerAddress);

        await signinPage.Open();
        await signinPage.AssertOpen();

        var signedInPage = await signinPage.SignIn();
        await signedInPage.AssertSignInSuccess();

        var state = await playwrightPage.Page.Context.StorageStateAsync(/*new() { Path = ".auth/state.json" }*/);
        if (string.IsNullOrEmpty(state))
            throw new InvalidOperationException("Authentication state is null or empty.");

        AuthenticationState = state.Replace(testServer.WebAppServerAddress.OriginalString.TrimEnd('/'), "[ServerAddress]");

        if (playwrightPage.Page.Context.Browser is IBrowser browser)
        {
            await browser.CloseAsync();
            await browser.DisposeAsync();
        }
    }
}
