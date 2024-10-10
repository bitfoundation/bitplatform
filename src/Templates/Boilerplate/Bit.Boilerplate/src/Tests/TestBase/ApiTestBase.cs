﻿using Microsoft.AspNetCore.Builder;

namespace Boilerplate.Tests.TestBase;

[TestClass]
public partial class ApiTestBase
{
    private AppTestServer TestServer = null!;
    public WebApplication WebApp => TestServer.WebApp;
    public Uri WebAppServerAddress => TestServer.WebAppServerAddress;

    [TestInitialize]
    public async Task InitializeTestServer()
    {
        TestServer = new AppTestServer();

        await TestServer.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();
    }

    [TestCleanup]
    public async ValueTask CleanupTestServer()
    {
        if (TestServer is not null)
        {
            await TestServer.DisposeAsync();
        }
    }
}
