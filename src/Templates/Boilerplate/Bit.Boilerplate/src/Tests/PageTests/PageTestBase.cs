﻿using System.Reflection;
using Boilerplate.Tests.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class PageTestBase : PageTest
{
    public AppTestServer TestServer { get; set; } = new();
    public WebApplication WebApp => TestServer.WebApp;
    public Uri WebAppServerAddress => TestServer.WebAppServerAddress;

    [TestInitialize]
    public async Task InitializeTestServer()
    {
        await Context.EnableBlazorWasmCaching();

        var currentTestMethod = GetType().GetMethod(TestContext.TestName!);

        var configureTestServer = currentTestMethod!.GetCustomAttribute<ConfigureTestServerAttribute>();
        if (configureTestServer is not null)
        {
            var configureTestServerMethod = GetType()
                .GetMethod(configureTestServer.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Method '{configureTestServer.MethodName}' not found.");
            var currentTestMethodArguments = GetTestMethodArguments();
            await (Task)configureTestServerMethod.Invoke(this, [TestServer, .. currentTestMethodArguments])!;
            return;
        }

        var autoStartTestServer = currentTestMethod!.GetCustomAttribute<AutoStartTestServerAttribute>();
        if (autoStartTestServer is null || autoStartTestServer.AutoStart)
        {
            await TestServer.Build().Start();
        }
    }

    [TestCleanup]
    public async ValueTask CleanupTestServer()
    {
        await Context.CloseAsync();
        if (TestContext.CurrentTestOutcome is not UnitTestOutcome.Failed)
        {
            var directory = GetVideoDirectory(TestContext);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        if (TestServer is not null)
        {
            await TestServer.DisposeAsync();
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.RecordVideoDir = GetVideoDirectory(TestContext);

        var currentTestMethod = GetType().GetMethod(TestContext.TestName!);

        var isAuthenticated = currentTestMethod!.GetCustomAttribute<AutoAuthenticateAttribute>() is not null;
        if (isAuthenticated)
        {
            options.StorageState = TestsInitializer.AuthenticationState.Replace("[ServerAddress]", WebAppServerAddress.OriginalString);
        }

        var configureBrowserContext = currentTestMethod!.GetCustomAttribute<ConfigureBrowserContextAttribute>();
        if (configureBrowserContext is not null)
        {
            var configureBrowserContextMethod = GetType()
                .GetMethod(configureBrowserContext.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Method '{configureBrowserContext.MethodName}' not found.");
            var currentTestMethodArguments = GetTestMethodArguments();
            configureBrowserContextMethod.Invoke(this, [options, .. currentTestMethodArguments]);
        }

        return options;
    }

    private string GetVideoDirectory(TestContext testContext)
    {
        var testMethodDsiplayName = GetTestMethodDisplayName();
        char[] invalidChars = [.. Path.GetInvalidPathChars(), .. Path.GetInvalidFileNameChars(), ')', '"', '<', '>', '|', '*', '?', '\r', '\n'];
        testMethodDsiplayName = new string(testMethodDsiplayName.Where(ch => !invalidChars.Contains(ch)).Select(ch => ch is '(' or ',' ? '_' : ch).ToArray());
        var testMethodFullName = $"{testContext.FullyQualifiedTestClassName}.{testMethodDsiplayName}";
        var dir = Path.Combine(testContext.TestResultsDirectory!, "..", "..", "Videos", testMethodFullName);
        return Path.GetFullPath(dir);
    }

    private string GetTestMethodDisplayName()
    {
        var testContextType = TestContext.GetType();
        var testMethodField = testContextType.GetField("_testMethod", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_testMethod' not found.");
        var testMethod = testMethodField.GetValue(TestContext) ?? throw new InvalidOperationException("Field '_testMethod' is null.");
        var testMethodType = testMethod.GetType();
        var displayNameProperty = testMethodType.GetProperty("DisplayName", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Property 'DisplayName' not found.");
        var displayName = displayNameProperty.GetValue(testMethod) ?? throw new InvalidOperationException("Field 'DisplayName' is null.");
        return (string)displayName;
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
