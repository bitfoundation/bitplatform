# Stage 17: Automated Testing (Unitigration Tests)

Welcome to **Stage 17**! In this stage, you'll learn about the comprehensive testing infrastructure built into this project. The project uses a hybrid approach called "**Unitigration Tests**" - tests that combine the **ease of writing unit tests** with the **real server behavior of integration tests**.

## What Are Unitigration Tests?

**Unitigration Tests** = **Ease** of writing unit tests + **Real** server behavior of integration tests

This is a pragmatic testing approach where tests are written as Integration Tests with full real server behavior (both UI tests and HTTP client-based tests), but with the flexibility to fake specific parts of the server when needed - similar to Unit Tests - making test writing much simpler.

**Key Characteristics:**
- Run against the **real application** with actual dependencies (database, services, middleware)
- Allow **selective mocking** of specific services when needed
- Provide **high confidence** like integration tests (real HTTP calls, real database)
- Support both **API testing** (backend logic) and **UI testing** (end-to-end with Playwright)

**Important Note:** Developers are welcome to write pure Unit Tests or pure Integration Tests if they prefer, but Unitigration Tests are recommended for most scenarios because this approach is superior to traditional unit tests that mock everything - it catches real integration issues while remaining maintainable and easy to write.

---

## Test Project Structure

The test project is located at [`src/Tests/Boilerplate.Tests.csproj`](/src/Tests/Boilerplate.Tests.csproj) and contains:

### Key Files:
- **[`TestsInitializer.cs`](/src/Tests/TestsInitializer.cs)**: Assembly-level setup that runs once before all tests
- **[`AppTestServer.cs`](/src/Tests/AppTestServer.cs)**: Core test infrastructure that spins up the web application
- **[`IdentityApiTests.cs`](/src/Tests/IdentityApiTests.cs)**: Example of API/backend testing
- **[`IdentityPagesTests.cs`](/src/Tests/IdentityPagesTests.cs)**: Example of UI testing with Playwright
- **[`TestData.cs`](/src/Tests/TestData.cs)**: Shared test data constants
- **[`.runsettings`](/src/Tests/.runsettings)**: Test configuration (Playwright settings, parallel execution, environment variables)

### Supporting Folders:
- **`Services/`**: Test-specific service implementations
  - [`TestAuthTokenProvider.cs`](/src/Tests/Services/TestAuthTokenProvider.cs): In-memory token provider for API tests
  - [`TestStorageService.cs`](/src/Tests/Services/TestStorageService.cs): In-memory storage for API tests
- **`Extensions/`**: Helper extensions
  - [`WebApplicationBuilderExtensions.cs`](/src/Tests/Extensions/WebApplicationBuilderExtensions.cs): Test service registration
  - [`PlaywrightVideoRecordingExtensions.cs`](/src/Tests/Extensions/PlaywrightVideoRecordingExtensions.cs): Video recording for failed tests

---

## Technologies Used

### Testing Frameworks:
- **MSTest v4**: The modern MSTest framework with native support for async, improved performance, and better diagnostics
- **Microsoft.Playwright.MSTest**: End-to-end browser automation for UI testing
- **Aspire.Hosting.Testing**: Integration with .NET Aspire for running dependencies (database, email, S3)
- **FakeItEasy**: Mocking library (when selective mocking is needed)

### Key Features:
- **Parallel Test Execution**: Tests run in parallel for faster feedback (configured in `.runsettings`)
- **Video Recording**: Failed UI tests automatically record videos for debugging
- **Real Dependencies**: Through Aspire, tests can run against real SQL Server, email server, etc.

---

## Core Test Infrastructure

### 1. AppTestServer - The Heart of Testing

The [`AppTestServer`](/src/Tests/AppTestServer.cs) class is responsible for spinning up the web application in-process for testing:

```csharp
public partial class AppTestServer : IAsyncDisposable
{
    public WebApplication WebApp { get; }
    public readonly Uri WebAppServerAddress = new(GenerateServerUrl());

    public AppTestServer Build(
        Action<IServiceCollection>? configureTestServices = null,
        Action<ConfigurationManager>? configureTestConfigurations = null)
    {
        // Creates a WebApplication with test-specific configuration
        // Allows overriding services and configuration
        // Returns the configured test server
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        await WebApp.StartAsync(cancellationToken);
    }
}
```

**Key Features:**
- **Dynamic Port Allocation**: Each test gets a unique port to avoid conflicts
- **Service Overriding**: Replace production services with test doubles
- **Configuration Overriding**: Modify appsettings for test scenarios
- **Full Application Stack**: All middleware, authentication, authorization, etc. work exactly as in production

### 2. TestsInitializer - Assembly Setup

The [`TestsInitializer`](/src/Tests/TestsInitializer.cs) runs **once** before all tests using `[AssemblyInitialize]`:

```csharp
[TestClass]
public partial class TestsInitializer
{
    [AssemblyInitialize]
    public static async Task Initialize(TestContext testContext)
    {
        await RunAspireHost(testContext);
        await using var testServer = new AppTestServer();
        await testServer.Build().Start(testContext.CancellationToken);
        await InitializeDatabase(testServer);
    }
}
```

**What It Does:**
1. **Starts .NET Aspire Host**: Spins up SQL Server, email server, S3, etc. (via Docker containers)
2. **Retrieves Connection Strings**: Gets database and service connection strings from Aspire
3. **Initializes Database**: Creates the database schema using EF Core
4. **Sets Environment Variables**: Makes connection strings available to all tests

**Important Note**: The Aspire app runs **without the web project** - only the infrastructure dependencies. The actual web application is started per-test using `AppTestServer`.

---

## API Testing Example

Let's examine [`IdentityApiTests.cs`](/src/Tests/IdentityApiTests.cs) to understand API testing:

```csharp
[TestClass]
public partial class IdentityApiTests
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            // Replace production services with test doubles
            services.Replace(ServiceDescriptor.Scoped<IStorageService, TestStorageService>());
            services.Replace(ServiceDescriptor.Transient<IAuthTokenProvider, TestAuthTokenProvider>());
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var authenticationManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        // Perform sign-in
        await authenticationManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        // Verify the signed-in user
        var user = await userController.GetCurrentUser(TestContext.CancellationToken);

        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.Id);
    }
}
```

**Key Concepts:**

### Service Replacement
```csharp
services.Replace(ServiceDescriptor.Scoped<IStorageService, TestStorageService>());
```
- **Why?** Browser storage doesn't exist in API tests, so we use an in-memory implementation
- **TestStorageService**: Simple `Dictionary<string, string?>` that mimics browser storage
- **Selective Mocking**: Only mock what's necessary; everything else is real

### Service Resolution
```csharp
await using var scope = server.WebApp.Services.CreateAsyncScope();
var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
```
- Create a DI scope just like in production
- Resolve services from the actual application container
- Services have access to real DbContext, configuration, etc.

### TestData Constants
```csharp
public const string DefaultTestEmail = "test@bitplatform.dev";
public const string DefaultTestPassword = "123456";
```
- Centralized test data in [`TestData.cs`](/src/Tests/TestData.cs)
- Seeds are created in `AppDbContext.OnModelCreating()` for development/testing
- Tests use known data for predictable assertions

---

## UI Testing Example (Playwright)

Let's examine [`IdentityPagesTests.cs`](/src/Tests/IdentityPagesTests.cs) for end-to-end UI testing:

```csharp
[TestClass]
public partial class IdentityPagesTests : PageTest
{
    [TestMethod]
    public async Task SignIn_Should_WorkAsExpected()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        // Navigate to sign-in page
        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.SignIn).ToString());

        // Verify page title
        await Expect(Page).ToHaveTitleAsync(AppStrings.SignInPageTitle);

        // Fill in credentials
        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(TestData.DefaultTestEmail);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(TestData.DefaultTestPassword);
        
        // Click sign-in button
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        // Verify successful sign-in
        await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = TestData.DefaultTestFullName })).ToBeVisibleAsync();
    }

    // Enable video recording for failed tests
    public override BrowserNewContextOptions ContextOptions() => 
        base.ContextOptions().EnableVideoRecording(TestContext);

    [TestCleanup]
    public async ValueTask Cleanup() => 
        await Context.FinalizeVideoRecording(TestContext);
}
```

**Key Concepts:**

### PageTest Base Class
- Inherit from `PageTest` (Microsoft.Playwright.MSTest)
- Automatically provides `Page` and `Context` properties
- Handles browser lifecycle (start/stop)

### Locator Best Practices
```csharp
await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue }).ClickAsync();
```
- **Use resource strings**: Same strings used in the app ensure tests break if UI text changes
- **Semantic locators**: `GetByRole`, `GetByPlaceholder` are more resilient than CSS selectors
- **Accessibility-friendly**: Tests enforce proper ARIA roles and labels

### Video Recording
```csharp
public override BrowserNewContextOptions ContextOptions() => 
    base.ContextOptions().EnableVideoRecording(TestContext);
```
- Videos are recorded in [`TestResults/Videos/`](/src/Tests/TestResults/Videos/)
- **Only failed tests** keep their videos (successful tests delete them to save space)
- Videos are invaluable for debugging intermittent failures

### Assertions
```csharp
await Expect(Page).ToHaveTitleAsync(AppStrings.SignInPageTitle);
await Expect(Page.GetByRole(AriaRole.Button, new() { Name = userFullName })).ToBeVisibleAsync();
```
- Playwright's `Expect` API with built-in retry logic
- Automatically waits for conditions (no explicit `Task.Delay`)
- Clear, readable assertions

---

## Test Configuration (.runsettings)

The [`.runsettings`](/src/Tests/.runsettings) file configures test execution:

```xml
<RunSettings>
    <Playwright>
        <LaunchOptions>
            <!--<Headless>true</Headless>-->
            <!--<SlowMo>1000</SlowMo>-->
        </LaunchOptions>
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

**Key Settings:**

### Parallel Execution
- **Workers**: 3 tests run simultaneously (configurable)
- **Scope**: MethodLevel (each test method runs independently)
- **Result**: Faster test execution without sacrificing isolation

### Headless Browser
- **Headless=true**: Browser runs without UI (faster, CI-friendly)
- **Commented out by default**: Developers can watch tests run locally
- **Debugging**: Uncomment `<PWDEBUG>1</PWDEBUG>` to debug with Playwright Inspector

### Environment Overrides
- **SQLite In-Memory**: Default for fast, isolated tests
- **Can Override**: Point to real SQL Server if needed
- **Hierarchy**: `.runsettings` → `appsettings.json` → environment variables

---

## Running Tests

### From Visual Studio:
1. Open **Test Explorer** (Test → Test Explorer)
2. Click **Run All** to execute all tests
3. Right-click individual tests to run/debug specific tests

### From Command Line:
```powershell
# Run all tests
dotnet test src/Tests/Boilerplate.Tests.csproj

# Run specific test
dotnet test src/Tests/Boilerplate.Tests.csproj --filter "FullyQualifiedName~SignInTest"

# Run with verbose output
dotnet test src/Tests/Boilerplate.Tests.csproj --logger "console;verbosity=detailed"
```

### From VS Code:
1. Install the **C# Dev Kit** extension
2. Tests appear in the **Testing** sidebar
3. Click the play button to run tests

### Playwright-Specific Commands:
```powershell
# Install Playwright browsers (first time only)
pwsh src/Tests/bin/Debug/net10.0/playwright.ps1 install

# Update Playwright browsers
pwsh src/Tests/bin/Debug/net10.0/playwright.ps1 install --force
```

---

## Test-Specific Services

### TestStorageService

Located at [`Services/TestStorageService.cs`](/src/Tests/Services/TestStorageService.cs):

```csharp
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
}
```

**Purpose:** In API tests, there's no browser storage, so this in-memory implementation stores tokens, preferences, etc.

### TestAuthTokenProvider

Located at [`Services/TestAuthTokenProvider.cs`](/src/Tests/Services/TestAuthTokenProvider.cs):

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

**Purpose:** Reads authentication tokens from `TestStorageService` instead of browser storage.

---

## Best Practices for Writing Tests

### 1. Test Isolation
- Each test should be **independent** and not rely on other tests
- Use `await using var server = new AppTestServer()` to get a fresh server per test
- Clean up resources with `IAsyncDisposable` pattern

### 2. Arrange-Act-Assert Pattern
```csharp
[TestMethod]
public async Task MyTest()
{
    // Arrange: Setup test server and dependencies
    await using var server = new AppTestServer();
    await server.Build().Start(TestContext.CancellationToken);

    // Act: Perform the operation being tested
    var result = await SomeOperation();

    // Assert: Verify the outcome
    Assert.AreEqual(expectedValue, result);
}
```

### 3. Use Resource Strings
```csharp
// Good: Uses actual app strings
await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);

// Bad: Hardcoded strings
await Page.GetByPlaceholder("Email").FillAsync(email);
```

### 4. Selective Mocking
- Only mock services that **must** be faked (e.g., external APIs, browser storage)
- Keep as much **real** infrastructure as possible (database, services, middleware)
- This catches real integration issues that pure unit tests miss

### 5. Meaningful Test Names
```csharp
// Good: Clearly describes what's being tested
[TestMethod]
public async Task SignIn_WithValidCredentials_Should_AuthenticateUser()

// Bad: Vague or generic names
[TestMethod]
public async Task Test1()
```

### 6. Handle Async Properly
```csharp
// Always await async operations
await authManager.SignIn(credentials, CancellationToken);

// Always pass CancellationToken from TestContext
TestContext.CancellationToken
```

---

## Debugging Tests

### Visual Studio Debugging:
1. Set a breakpoint in your test method
2. Right-click the test in Test Explorer
3. Select **Debug** → Debugger attaches to the running test

### Playwright Debugging:
1. Uncomment `<PWDEBUG>1</PWDEBUG>` in `.runsettings`
2. Run the test → Playwright Inspector opens
3. Step through UI interactions, inspect selectors, view screenshots

### Video Debugging:
1. Failed UI tests automatically save videos in `TestResults/Videos/`
2. Each test gets its own folder with a video file
3. Watch the video to see exactly what happened

### Console Output:
```csharp
// Add logging to understand test flow
Console.WriteLine($"Signing in with {email}");
```

---

## Common Testing Scenarios

### Testing Authorization
```csharp
[TestMethod]
public async Task UnauthorizedUser_Should_GetUnauthorizedException()
{
    await using var server = new AppTestServer();
    await server.Build(services =>
    {
        services.Replace(ServiceDescriptor.Scoped<IStorageService, TestStorageService>());
        services.Replace(ServiceDescriptor.Transient<IAuthTokenProvider, TestAuthTokenProvider>());
    }).Start(TestContext.CancellationToken);

    await using var scope = server.WebApp.Services.CreateAsyncScope();
    var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

    // Should throw because user is not authenticated
    await Assert.ThrowsExactlyAsync<UnauthorizedException>(
        () => userController.GetCurrentUser(TestContext.CancellationToken)
    );
}
```

### Testing Validation
```csharp
[TestMethod]
public async Task CreateUser_WithInvalidEmail_Should_FailValidation()
{
    await using var server = new AppTestServer();
    await server.Build().Start(TestContext.CancellationToken);

    await using var scope = server.WebApp.Services.CreateAsyncScope();
    var controller = scope.ServiceProvider.GetRequiredService<IUserController>();

    var exception = await Assert.ThrowsExactlyAsync<ResourceValidationException>(
        () => controller.Create(new UserDto { Email = "invalid-email" }, CancellationToken)
    );

    Assert.IsTrue(exception.Message.Contains("Email"));
}
```

### Testing UI Navigation
```csharp
[TestMethod]
public async Task ClickingLogo_Should_NavigateToHome()
{
    await using var server = new AppTestServer();
    await server.Build().Start(TestContext.CancellationToken);

    await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.About).ToString());
    
    await Page.GetByRole(AriaRole.Link, new() { Name = "Logo" }).ClickAsync();
    
    await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
}
```

---

## Continuous Integration (CI)

The project includes GitHub Actions workflows that run tests automatically:

### On Every Push/PR:
- Tests run in parallel on CI servers
- Playwright browsers are cached for speed
- Test results are uploaded as artifacts
- Failed test videos are saved for review

### Configuration in `.github/workflows/`:
```yaml
- name: Run Tests
  run: dotnet test src/Tests/Boilerplate.Tests.csproj --configuration Release

- name: Upload Test Videos (on failure)
  if: failure()
  uses: actions/upload-artifact@v4
  with:
    name: test-videos
    path: src/Tests/TestResults/Videos/
```

---

## Advanced Topics

### Custom Test Server Configuration
```csharp
await server.Build(
    configureTestServices: services =>
    {
        // Replace services
        services.Replace(ServiceDescriptor.Singleton<IEmailService, FakeEmailService>());
    },
    configureTestConfigurations: config =>
    {
        // Override configuration
        config["IdentitySettings:PasswordRequiredLength"] = "4";
    }
).Start(TestContext.CancellationToken);
```

---

## Why Unitigration Tests Are Superior

### Traditional Unit Tests (Pure Mocking):
❌ Mock DbContext → Tests don't catch EF Core query issues  
❌ Mock HttpClient → Tests don't catch serialization issues  
❌ Mock everything → Tests become fragile and test implementation details  
❌ False confidence → Tests pass but production breaks  

### Traditional Integration Tests (E2E):
❌ Slow → Require external infrastructure (database servers, Docker)  
❌ Flaky → Network issues, timing problems  
❌ Complex setup → Hard to maintain  
❌ Hard to debug → Multiple moving parts  

### Unitigration Tests (This Project):
✅ **Reliable** → Real code paths, no network  
✅ **Easy to debug** → Single process, standard debugging  
✅ **High confidence** → Real middleware, authentication, validation  
✅ **Flexible** → Selectively mock only what's necessary  
✅ **Production-like** → Same code paths as production  

---

## Summary

You've learned about the comprehensive testing infrastructure in this project:

1. **Unitigration Tests**: Hybrid approach combining speed and reliability
2. **AppTestServer**: In-process web server for fast, isolated tests
3. **API Testing**: Test backend logic with real dependencies
4. **UI Testing**: End-to-end tests with Playwright
5. **Test Configuration**: `.runsettings` for customization
6. **Best Practices**: Isolation, meaningful names, selective mocking
7. **Debugging Tools**: Video recording, Playwright Inspector, standard debugging

The testing approach in this project gives you **confidence** that your code works correctly while keeping tests **fast** and **maintainable**.

---

## Additional Resources

- **Playwright Documentation**: https://playwright.dev/dotnet/
- **MSTest Documentation**: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro
- **Aspire Testing**: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing
- **FakeItEasy Documentation**: https://fakeiteasy.github.io/

---
