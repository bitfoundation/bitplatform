# Stage 17: Automated Testing (Unitigration Tests)

Welcome to Stage 17! In this stage, you'll learn about the comprehensive testing architecture built into this project, which combines the best of both unit testing and integration testing approaches—often called "Unitigration Tests."

## What Are Unitigration Tests?

Traditional testing approaches have distinct characteristics:

- **Unit Tests**: Fast, isolated, but often test code in unrealistic conditions with many mocks
- **Integration Tests**: More realistic, but slower and more complex to set up

**Unitigration Tests** combine the best of both worlds:
- ✅ Test **real application behavior** with minimal mocking
- ✅ Run against a **real database** (in-memory for speed)
- ✅ Test **real Blazor UI components** using Playwright
- ✅ Test **real API endpoints** without mocking HTTP
- ✅ **Fast execution** suitable for CI/CD pipelines
- ✅ **Easy to write** and maintain

This approach gives you confidence that your application actually works, not just that mocked interfaces are called correctly.

---

## Project Structure

The testing infrastructure lives in the **`Boilerplate.Tests`** project:

```
src/Tests/
├── .runsettings                           # Test execution configuration
├── AppTestServer.cs                       # In-memory test server setup
├── Boilerplate.Tests.csproj              # Test project configuration
├── IdentityApiTests.cs                    # API endpoint tests
├── IdentityPagesTests.cs                  # Blazor UI tests with Playwright
├── TestData.cs                            # Shared test data constants
├── TestsInitializer.cs                    # Test environment setup
├── Extensions/
│   ├── PlaywrightVideoRecordingExtensions.cs  # Video capture for UI tests
│   └── WebApplicationBuilderExtensions.cs      # Test service configuration
└── Services/
    ├── TestAuthTokenProvider.cs          # Test authentication provider
    └── TestStorageService.cs             # Test storage implementation
```

---

## Testing Technologies

The project uses industry-standard testing frameworks:

### Core Testing Frameworks
- **MSTest**: Microsoft's testing framework ([`Boilerplate.Tests.csproj`](/src/Tests/Boilerplate.Tests.csproj))
  - Modern, fast, and well-integrated with Visual Studio and VS Code
  - Supports parallel test execution
  - Includes `[TestClass]`, `[TestMethod]`, `Assert` APIs

### API Testing
- **AppTestServer**: Custom in-memory test server
  - Runs the full ASP.NET Core application in-memory
  - Uses real controllers, services, and database
  - No HTTP overhead—tests run directly in-process
  - Automatically finds available ports to avoid conflicts

### UI Testing with Playwright
- **Microsoft Playwright**: Modern browser automation ([`IdentityPagesTests.cs`](/src/Tests/IdentityPagesTests.cs))
  - Cross-browser support (Chromium, Firefox, WebKit)
  - Fast and reliable
  - Automatic waiting and retry logic
  - Video recording on test failures
  - Detailed error messages with screenshots

### Mocking (When Needed)
- **FakeItEasy**: Simple, flexible mocking library
  - Used sparingly—only for services that can't run in tests (e.g., real SMS providers)
  - Clean syntax compared to other mocking frameworks

---

## Test Initialization: TestsInitializer.cs

The [`TestsInitializer.cs`](/src/Tests/TestsInitializer.cs) file runs **once per test session** and sets up the testing environment.

### What It Does

1. **Starts Aspire Dependencies** (if using .NET Aspire):
   - Launches containerized dependencies (database, blob storage, SMTP)
   - Extracts connection strings and makes them available to tests
   - Ensures all infrastructure is ready before tests run

2. **Initializes the Test Server**:
   - Creates the first instance of `AppTestServer`
   - Ensures the database is created (using `EnsureCreatedAsync()` for tests)

3. **SQLite In-Memory Mode**:
   - For SQLite databases, keeps a connection open throughout the test session
   - This prevents the in-memory database from being destroyed between tests

### Key Code Snippet

```csharp
[AssemblyInitialize]
public static async Task Initialize(TestContext testContext)
{
    // Start Aspire dependencies if enabled
    await RunAspireHost(testContext);
    
    // Create and start test server
    await using var testServer = new AppTestServer();
    await testServer.Build().Start(testContext.CancellationToken);
    
    // Initialize database
    await InitializeDatabase(testServer);
}
```

### Why `EnsureCreatedAsync()` Instead of Migrations?

In **production code**, you should always use EF Core migrations. However, for **tests**, using `EnsureCreatedAsync()` is acceptable and faster because:
- Test databases are ephemeral (created fresh for each test session)
- You don't need migration history tracking
- Schema changes are immediately applied without creating new migration files

---

## AppTestServer: The Heart of Testing

The [`AppTestServer.cs`](/src/Tests/AppTestServer.cs) class creates an in-memory instance of your ASP.NET Core application.

### Key Features

```csharp
public partial class AppTestServer : IAsyncDisposable
{
    public WebApplication WebApp { get; }
    public readonly Uri WebAppServerAddress;

    public AppTestServer Build(
        Action<IServiceCollection>? configureTestServices = null,
        Action<ConfigurationManager>? configureTestConfigurations = null)
    {
        // Creates a real ASP.NET Core application
        var builder = WebApplication.CreateBuilder();
        
        // Auto-generates unique URL (e.g., http://127.0.0.1:54321/)
        builder.Configuration["ServerAddress"] = WebAppServerAddress.ToString();
        builder.WebHost.UseUrls(WebAppServerAddress.ToString());
        
        // Apply test-specific overrides
        configureTestServices?.Invoke(builder.Services);
        configureTestConfigurations?.Invoke(builder.Configuration);
        
        return this;
    }
}
```

### Why This Approach?

- **Real Application**: Your tests run against the actual app, not a fake version
- **Service Overrides**: Replace specific services (e.g., swap real SMS sender with a test version)
- **Configuration Overrides**: Change settings (e.g., use in-memory database)
- **No Network Overhead**: Runs in-process—much faster than starting a real web server
- **Port Collision Avoidance**: Automatically finds an available port

---

## API Testing: IdentityApiTests.cs

The [`IdentityApiTests.cs`](/src/Tests/IdentityApiTests.cs) file demonstrates how to test API endpoints.

### Example: Sign-In Test

```csharp
[TestClass]
public partial class IdentityApiTests
{
    [TestMethod]
    public async Task SignInTest()
    {
        // Create and start test server
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            // Override specific services for testing
            services.Replace(ServiceDescriptor.Scoped<IStorageService, TestStorageService>());
            services.Replace(ServiceDescriptor.Transient<IAuthTokenProvider, TestAuthTokenProvider>());
        }).Start(TestContext.CancellationToken);

        // Create a service scope (like a request scope)
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // Get services from DI container
        var authenticationManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        // Execute sign-in (calls real API controller)
        await authenticationManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        // Get user controller and fetch current user
        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();
        var user = await userController.GetCurrentUser(TestContext.CancellationToken);

        // Assert expected result
        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.Id);
    }
}
```

### What's Happening Here?

1. **Server Setup**: Creates an in-memory ASP.NET Core application
2. **Service Overrides**: Replaces `IStorageService` and `IAuthTokenProvider` with test versions
3. **Real Sign-In Flow**: 
   - Calls the actual `AuthManager.SignIn` method
   - Executes real API controller logic
   - Accesses real database
   - Stores authentication token in test storage
4. **Verification**: Fetches the current user and verifies the ID

### Example: Unauthorized Access Test

```csharp
[TestMethod]
public async Task UnauthorizedAccessTest()
{
    await using var server = new AppTestServer();

    await server.Build(services =>
    {
        services.Replace(ServiceDescriptor.Scoped<IStorageService, TestStorageService>());
        services.Replace(ServiceDescriptor.Transient<IAuthTokenProvider, TestAuthTokenProvider>());
    }).Start(TestContext.CancellationToken);

    await using var scope = server.WebApp.Services.CreateAsyncScope();

    var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

    // Should throw UnauthorizedException because user is not signed in
    await Assert.ThrowsExactlyAsync<UnauthorizedException>(
        () => userController.GetCurrentUser(TestContext.CancellationToken)
    );
}
```

This test verifies that authorization is working correctly—unauthenticated users cannot access protected endpoints.

---

## UI Testing with Playwright: IdentityPagesTests.cs

The [`IdentityPagesTests.cs`](/src/Tests/IdentityPagesTests.cs) file demonstrates how to test Blazor UI components using Playwright.

### Setup: Inherit from PageTest

```csharp
[TestClass]
public partial class IdentityPagesTests : PageTest
{
    // PageTest provides: Page, Browser, Context
}
```

The `PageTest` base class from Playwright provides:
- `Page`: The browser page to interact with
- `Browser`: The browser instance
- `Context`: The browser context (like an incognito window)

### Example: Unauthorized User Test

```csharp
[TestMethod]
public async Task UnauthorizedUser_Should_RenderNotAuthorizedComponent()
{
    await using var server = new AppTestServer();
    await server.Build().Start(TestContext.CancellationToken);

    // Navigate to protected page
    await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Settings).ToString());

    // Verify "Not Authorized" page is shown
    await Expect(Page).ToHaveTitleAsync(AppStrings.NotAuthorizedPageTitle);
}
```

This test:
1. Starts the test server
2. Navigates to a protected page (`/settings`)
3. Verifies that unauthorized users see the "Not Authorized" page

### Example: Sign-In Flow Test

```csharp
[TestMethod]
public async Task SignIn_Should_WorkAsExpected()
{
    await using var server = new AppTestServer();
    await server.Build().Start(TestContext.CancellationToken);

    // Navigate to sign-in page
    await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.SignIn).ToString());

    // Verify page title
    await Expect(Page).ToHaveTitleAsync(AppStrings.SignInPageTitle);

    // Fill in sign-in form
    await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(TestData.DefaultTestEmail);
    await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(TestData.DefaultTestPassword);
    
    // Click sign-in button
    await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

    // Verify successful sign-in
    await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
    await Expect(Page.GetByRole(AriaRole.Button, new() { Name = TestData.DefaultTestFullName })).ToBeVisibleAsync();
    await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(TestData.DefaultTestFullName);
}
```

### Playwright Best Practices Used Here

✅ **Auto-Waiting**: Playwright automatically waits for elements to be ready  
✅ **Accessibility Selectors**: Uses `GetByRole`, `GetByPlaceholder` for robust element selection  
✅ **Assertions with Timeout**: `Expect()` automatically retries until condition is met  
✅ **Localization-Friendly**: Uses `AppStrings` instead of hardcoded text

### Video Recording for Failed Tests

```csharp
public override BrowserNewContextOptions ContextOptions() 
    => base.ContextOptions().EnableVideoRecording(TestContext);

[TestCleanup]
public async ValueTask Cleanup() 
    => await Context.FinalizeVideoRecording(TestContext);
```

This configuration:
- Records video of all tests
- **Keeps videos only for failed tests** (saves disk space)
- Stores videos in `src/Tests/TestResults/Videos/{TestMethodName}/`

When a test fails, you can watch the video to see exactly what went wrong—this is incredibly useful for debugging flaky tests or understanding failure scenarios.

---

## Test Service Overrides

### TestStorageService

Located in [`Services/TestStorageService.cs`](/src/Tests/Services/TestStorageService.cs):

```csharp
/// <summary>
/// In UI tests, browser uses its own storage, but for API tests, we need to fake the storage.
/// </summary>
public partial class TestStorageService : IStorageService
{
    private readonly Dictionary<string, string?> tempStorage = [];

    public async ValueTask<string?> GetItem(string key)
    {
        tempStorage.TryGetValue(key, out string? value);
        return value;
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (tempStorage.TryAdd(key, value) is false)
        {
            tempStorage[key] = value;
        }
    }

    // ... other methods
}
```

**Why?**
- In API tests, there's no browser to provide localStorage/sessionStorage
- This in-memory dictionary simulates storage for authentication tokens

### TestAuthTokenProvider

Located in [`Services/TestAuthTokenProvider.cs`](/src/Tests/Services/TestAuthTokenProvider.cs):

```csharp
public partial class TestAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IStorageService storageService = default!;

    public async Task<string?> GetAccessToken()
    {
        return await storageService.GetItem("access_token");
    }
}
```

**Why?**
- Reads authentication tokens from `TestStorageService`
- Allows API tests to authenticate without a browser

---

## Test Configuration: .runsettings

The [`.runsettings`](/src/Tests/.runsettings) file configures test execution:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
    <Playwright>
        <LaunchOptions>
            <!--<Headless>true</Headless>-->
            <!--<SlowMo>1000</SlowMo>-->
        </LaunchOptions>
        <!--<BrowserName>chromium</BrowserName>-->
    </Playwright>

    <MSTest>
        <Parallelize>
            <Workers>3</Workers>
            <Scope>MethodLevel</Scope>
        </Parallelize>
    </MSTest>

    <RunConfiguration>
        <EnvironmentVariables>
            <!-- Override appsettings for tests -->
            <ConnectionStrings__sqlite>Data Source=BoilerplateDb.db;Mode=Memory;Cache=Shared;</ConnectionStrings__sqlite>
        </EnvironmentVariables>
    </RunConfiguration>
</RunSettings>
```

### Key Settings

1. **Playwright Configuration**:
   - `Headless`: Run tests without opening browser window (faster in CI/CD)
   - `SlowMo`: Slow down actions (useful for debugging)
   - `BrowserName`: Choose browser (chromium, firefox, webkit)

2. **Parallel Execution**:
   - `Workers: 3`: Run up to 3 tests simultaneously
   - `Scope: MethodLevel`: Each test method can run in parallel

3. **Environment Variables**:
   - Override configuration for tests
   - Example: Use in-memory SQLite for faster tests

---

## Running Tests

### Visual Studio / VS Code
1. Open Test Explorer
2. Click "Run All" or run individual tests
3. View results and debug failures

### Command Line

```bash
# Run all tests
dotnet test src/Tests

# Run specific test class
dotnet test src/Tests --filter FullyQualifiedName~IdentityApiTests

# Run specific test method
dotnet test src/Tests --filter FullyQualifiedName~SignInTest

# Run with detailed output
dotnet test src/Tests --logger "console;verbosity=detailed"
```

### CI/CD Pipeline
Tests automatically run in GitHub Actions/Azure DevOps on every commit.

---

## Debugging Tests

### Debug in Visual Studio/VS Code
1. Set breakpoint in test method
2. Right-click test → "Debug Test"
3. Step through code as normal

### Debug Playwright Tests
Uncomment in `.runsettings`:
```xml
<Headless>false</Headless>  <!-- See browser -->
<SlowMo>1000</SlowMo>        <!-- Slow down actions -->
```

Or use environment variable:
```xml
<EnvironmentVariables>
    <PWDEBUG>1</PWDEBUG>  <!-- Opens Playwright Inspector -->
</EnvironmentVariables>
```

### Watch Test Videos
Failed test videos are saved in:
```
src/Tests/TestResults/Videos/{TestClassName}.{TestMethodName}/
```

---

## Writing Your Own Tests

### 1. API Test Template

```csharp
[TestClass]
public partial class MyApiTests
{
    [TestMethod]
    public async Task MyTest()
    {
        // Arrange: Setup test server
        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            // Override services if needed
            // services.Replace(ServiceDescriptor.Scoped<IMyService, TestMyService>());
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // Act: Execute the code you want to test
        var myController = scope.ServiceProvider.GetRequiredService<IMyController>();
        var result = await myController.MyMethod(TestContext.CancellationToken);

        // Assert: Verify the result
        Assert.IsNotNull(result);
        Assert.AreEqual("expected value", result.SomeProperty);
    }

    public TestContext TestContext { get; set; } = default!;
}
```

### 2. UI Test Template

```csharp
[TestClass]
public partial class MyPageTests : PageTest
{
    [TestMethod]
    public async Task MyUiTest()
    {
        // Arrange: Setup test server
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        // Act: Navigate and interact with UI
        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.MyPage).ToString());
        await Page.GetByRole(AriaRole.Button, new() { Name = "My Button" }).ClickAsync();

        // Assert: Verify UI state
        await Expect(Page.GetByText("Success!")).ToBeVisibleAsync();
    }

    public override BrowserNewContextOptions ContextOptions() 
        => base.ContextOptions().EnableVideoRecording(TestContext);

    [TestCleanup]
    public async ValueTask Cleanup() 
        => await Context.FinalizeVideoRecording(TestContext);

    public TestContext TestContext { get; set; } = default!;
}
```

---

## Best Practices

### ✅ DO:
- Test real user scenarios, not internal implementation details
- Use meaningful test names that describe what you're testing
- Keep tests independent—each test should set up its own data
- Use `TestData` class for shared test constants
- Override only the services you need to fake
- Use Playwright's accessibility selectors (`GetByRole`, `GetByLabel`)
- Let Playwright auto-wait—don't add manual delays

### ❌ DON'T:
- Mock everything (defeats the purpose of integration testing)
- Share state between tests (causes flaky tests)
- Hardcode URLs or text (use `PageUrls` and `AppStrings`)
- Use CSS selectors unless absolutely necessary
- Add `Thread.Sleep()` or `Task.Delay()` (use Playwright's `Expect()` instead)

---

## Troubleshooting

### "Test server port already in use"
- The test automatically finds an available port
- If still failing, check for processes holding ports

### "Database is locked" (SQLite)
- Ensure `TestsInitializer` keeps the SQLite connection open
- Check that tests properly dispose `AppTestServer` using `await using`

### "Element not found" in Playwright tests
- Use `await Expect(element).ToBeVisibleAsync()` instead of direct assertions
- Check that you're using the correct locator strategy
- Verify the element exists in pre-rendered output

### Tests are slow
- Ensure `.runsettings` has `<Headless>true</Headless>`
- Use in-memory databases (SQLite with `Mode=Memory`)
- Run tests in parallel (already configured in `.runsettings`)

### Video recording fills disk space
- Videos are automatically deleted for passing tests
- Manually delete `src/Tests/TestResults/Videos/` periodically

---

## Key Takeaways

✅ **Unitigration Tests** combine the speed of unit tests with the confidence of integration tests  
✅ **AppTestServer** runs your real application in-memory for fast, realistic tests  
✅ **Playwright** provides reliable, cross-browser UI testing with automatic waiting  
✅ **Minimal Mocking** means your tests verify actual behavior, not mock interactions  
✅ **Video Recording** makes debugging failed tests much easier  
✅ **Parallel Execution** keeps test suites fast even as they grow  

This testing approach gives you **confidence that your application actually works** while remaining **fast enough for continuous integration**.

---

## Additional Resources

- **MSTest Documentation**: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest
- **Playwright for .NET**: https://playwright.dev/dotnet/
- **FakeItEasy**: https://fakeiteeasy.github.io/
- **.NET Aspire Testing**: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing

---

**Do you have any questions about the testing approach or writing tests, or shall we proceed to Stage 18 (Other Available Prompt Templates)?**
