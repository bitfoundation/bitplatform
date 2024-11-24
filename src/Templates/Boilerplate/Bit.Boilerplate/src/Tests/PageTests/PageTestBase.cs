using System.Reflection;
using Boilerplate.Client.Web;
using Boilerplate.Tests.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public abstract partial class PageTestBase : PageTest
{
    public AppTestServer TestServer { get; set; } = new();
    public WebApplication WebApp => TestServer.WebApp;
    public virtual Uri WebAppServerAddress => TestServer.WebAppServerAddress;
    public virtual BlazorWebAppMode BlazorRenderMode => BlazorWebAppMode.BlazorServer;
    public virtual bool PreRenderEnabled => false;
    public virtual bool EnableBlazorWasmCaching => true;

    [TestInitialize]
    public async Task InitializeTestServer()
    {
        if (PreRenderEnabled)
            await Context.EnableHydrationCheck();

        if (EnableBlazorWasmCaching)
            await Context.EnableBlazorWasmCaching();

        await Context.SetBlazorWebAssemblyServerAddress(WebAppServerAddress.OriginalString);

        var currentTestMethod = GetType().GetMethod(TestContext.TestName!);

        var configureTestServer = currentTestMethod!.GetCustomAttribute<ConfigureTestServerAttribute>();
        if (configureTestServer is not null)
        {
            var configureTestServerMethod = GetType()
                .GetMethod(configureTestServer.MethodName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Method '{configureTestServer.MethodName}' not found.");
            var currentTestMethodArguments = GetTestMethodArguments();
            await (Task)configureTestServerMethod.Invoke(this, [TestServer, .. currentTestMethodArguments])!;
            return;
        }

        var autoStartTestServer = currentTestMethod!.GetCustomAttribute<AutoStartTestServerAttribute>();
        if (autoStartTestServer?.AutoStart != false)
        {
            await TestServer.Build(configureTestConfigurations: configuration =>
            {
                configuration["WebAppRender:PrerenderEnabled"] = PreRenderEnabled.ToString();
                configuration["WebAppRender:BlazorMode"] = BlazorRenderMode.ToString();
            }).Start();
        }
    }

    [TestCleanup]
    public async ValueTask CleanupTestServer()
    {
        await Context.FinalizeVideoRecording(TestContext);

        if (TestServer is not null)
        {
            await TestServer.DisposeAsync();
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.EnableVideoRecording(TestContext);

        var currentTestMethod = GetType().GetMethod(TestContext.TestName!);

        var isAuthenticated = currentTestMethod!.GetCustomAttribute<AutoAuthenticateAttribute>() is not null;
        if (isAuthenticated)
        {
            options.StorageState = TestsInitializer.AuthenticationState.Replace("[ServerAddress]", WebAppServerAddress.ToString());
        }

        var configureBrowserContext = currentTestMethod!.GetCustomAttribute<ConfigureBrowserContextAttribute>();
        if (configureBrowserContext is not null)
        {
            var configureBrowserContextMethod = GetType()
                .GetMethod(configureBrowserContext.MethodName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Method '{configureBrowserContext.MethodName}' not found.");
            var currentTestMethodArguments = GetTestMethodArguments();
            configureBrowserContextMethod.Invoke(this, [options, .. currentTestMethodArguments]);
        }

        return options;
    }

    private string?[]? GetTestMethodArguments()
    {
        var testContextType = TestContext.GetType();
        var testMethodField = testContextType.GetField("_testMethod", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_testMethod' not found.");
        var testMethod = testMethodField.GetValue(TestContext) ?? throw new InvalidOperationException("Field '_testMethod' is null.");
        var testMethodType = testMethod.GetType();
        var serializedDataProperty = testMethodType.GetProperty("SerializedData", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Property 'SerializedData' not found.");
        var serializedData = (string?[]?)serializedDataProperty.GetValue(testMethod);
        if (serializedData is null) return [];
        return serializedData.Where((_, index) => index % 2 == 1).Select(x => x?.Trim('"')).ToArray();
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AutoAuthenticateAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConfigureBrowserContextAttribute(string methodName) : Attribute
{
    public string MethodName => methodName;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConfigureTestServerAttribute(string methodName) : Attribute
{
    public string MethodName => methodName;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AutoStartTestServerAttribute(bool autoStart = true) : Attribute
{
    public bool AutoStart => autoStart;
}
