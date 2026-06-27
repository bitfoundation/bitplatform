using System;
using System.Linq;
using System.Reflection;
using Bit.BlazorUI;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Guards <see cref="IJSRuntimeExtensions.IsRuntimeInvalid"/> against silent breakage when ASP.NET Core
/// renames internal <see cref="IJSRuntime"/> implementations or the reflection targets they rely on.
/// </summary>
[TestClass]
public class IsRuntimeInvalidFrameworkContractTests
{
    [TestMethod]
    public void FrameworkRuntimeTypes_ShouldMatchIsRuntimeInvalidReflectionContract()
    {
        AssertFrameworkTypeContract(
            assemblyName: "Microsoft.AspNetCore.Components.Endpoints",
            typeName: "UnsupportedJavaScriptRuntime",
            upstreamSource: "https://github.com/dotnet/aspnetcore/blob/main/src/Components/Endpoints/src/DependencyInjection/UnsupportedJavaScriptRuntime.cs");

        AssertFrameworkTypeContract(
            assemblyName: "Microsoft.AspNetCore.Components.Server",
            typeName: "RemoteJSRuntime",
            propertyName: "IsInitialized",
            propertyType: typeof(bool),
            upstreamSource: "https://github.com/dotnet/aspnetcore/blob/main/src/Components/Server/src/Circuits/RemoteJSRuntime.cs");

        AssertFrameworkTypeContract(
            assemblyName: "Microsoft.AspNetCore.Components.WebView",
            typeName: "WebViewJSRuntime",
            fieldName: "_ipcSender",
            upstreamSource: "https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebView/WebView/src/Services/WebViewJSRuntime.cs");
    }

    [TestMethod]
    public void IsRuntimeInvalid_WhenFrameworkPrerenderRuntime_ShouldReturnTrue()
    {
        var runtime = CreateFrameworkRuntime("Microsoft.AspNetCore.Components.Endpoints", "UnsupportedJavaScriptRuntime");

        Assert.IsTrue(((IJSRuntime)runtime).IsRuntimeInvalid());
    }

    [TestMethod]
    public void IsRuntimeInvalid_WhenFrameworkHybridRuntimeNotAttached_ShouldReturnTrue()
    {
        var runtime = CreateFrameworkRuntime("Microsoft.AspNetCore.Components.WebView", "WebViewJSRuntime");

        Assert.IsTrue(((IJSRuntime)runtime).IsRuntimeInvalid());
    }

    private static void AssertFrameworkTypeContract(
        string assemblyName,
        string typeName,
        string upstreamSource,
        string? propertyName = null,
        Type? propertyType = null,
        string? fieldName = null)
    {
        var type = ResolveFrameworkRuntimeType(assemblyName, typeName);

        Assert.AreEqual(typeName, type.Name,
            $"Expected the framework runtime simple name to remain '{typeName}'. " +
            $"If ASP.NET Core renamed it, update IsRuntimeInvalid and this test. Source: {upstreamSource}");

        if (propertyName is not null)
        {
            var property = type.GetProperty(propertyName);
            Assert.IsNotNull(property,
                $"Expected '{type.FullName}' to expose '{propertyName}'. " +
                $"If ASP.NET Core removed or renamed it, update IsRuntimeInvalid. Source: {upstreamSource}");

            if (propertyType is not null)
            {
                Assert.AreEqual(propertyType, property!.PropertyType,
                    $"Expected '{type.FullName}.{propertyName}' to remain '{propertyType.Name}'. Source: {upstreamSource}");
            }
        }

        if (fieldName is not null)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field,
                $"Expected '{type.FullName}' to expose private field '{fieldName}'. " +
                $"If ASP.NET Core removed or renamed it, update IsRuntimeInvalid. Source: {upstreamSource}");
        }
    }

    private static object CreateFrameworkRuntime(string assemblyName, string typeName)
    {
        var type = ResolveFrameworkRuntimeType(assemblyName, typeName);
        return Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException($"Could not create an instance of '{type.FullName}'.");
    }

    private static Type ResolveFrameworkRuntimeType(string assemblyName, string typeName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal))
            ?? Assembly.Load(assemblyName);

        var type = assembly.GetTypes().SingleOrDefault(t => t.Name == typeName);
        Assert.IsNotNull(type,
            $"Could not find framework runtime '{typeName}' in assembly '{assemblyName}'. " +
            "Ensure the test project references the matching ASP.NET Core Components package.");

        Assert.IsTrue(typeof(IJSRuntime).IsAssignableFrom(type!),
            $"Expected '{type!.FullName}' to implement {nameof(IJSRuntime)}.");

        return type;
    }
}
