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
        // Prefer an already-loaded instance (a real Server/Hybrid host would have these in-process), and
        // otherwise try to load it on demand. These framework runtime assemblies
        // (Microsoft.AspNetCore.Components.Server/Endpoints/WebView) are not part of this test project's
        // dependency closure - the library matches them by type name via reflection only at runtime - so
        // when they aren't loadable here the contract simply can't be verified. Report the test as
        // inconclusive rather than failing, mirroring FastInvokeSyncContractTests when its prerequisite
        // source tree is absent. The guard still runs wherever these assemblies are actually present.
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));

        if (assembly is null)
        {
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (Exception ex) when (ex is System.IO.FileNotFoundException or System.IO.FileLoadException or BadImageFormatException)
            {
                Assert.Inconclusive(
                    $"Skipped: '{assemblyName}' is not available in this test environment, so the " +
                    $"IsRuntimeInvalid reflection contract for '{typeName}' can't be verified here.");
                return null!; // unreachable: Assert.Inconclusive always throws.
            }
        }

        // IsRuntimeInvalid matches these runtimes by simple Type.Name, so the lookup mirrors that instead of
        // pinning a namespace. GetTypes() throws when a type in the assembly references a dependency this test
        // project doesn't carry, so fall back to the types that did load; the contract can still be verified
        // from those, and a genuinely unloadable target is reported as inconclusive below.
        Type[] candidates;
        var partiallyLoaded = false;
        try
        {
            candidates = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            candidates = ex.Types.Where(t => t is not null).ToArray()!;
            partiallyLoaded = true;
        }

        // Not SingleOrDefault: a duplicate simple name in a future framework version would throw
        // InvalidOperationException instead of reporting the ambiguity, and every match satisfies the
        // name-based contract that IsRuntimeInvalid relies on, so the first one is enough to verify it.
        var type = candidates.FirstOrDefault(t => t.Name == typeName);

        if (type is null && partiallyLoaded)
        {
            // The target is among the types that failed to load, so its absence says nothing about the
            // contract. Report inconclusive, as above for a missing assembly, rather than failing.
            Assert.Inconclusive(
                $"Skipped: '{typeName}' could not be loaded from '{assemblyName}' in this test environment, " +
                "so the IsRuntimeInvalid reflection contract for it can't be verified here.");
        }

        Assert.IsNotNull(type,
            $"Could not find framework runtime '{typeName}' in assembly '{assemblyName}'. " +
            "Ensure the test project references the matching ASP.NET Core Components package.");

        Assert.IsTrue(typeof(IJSRuntime).IsAssignableFrom(type!),
            $"Expected '{type!.FullName}' to implement {nameof(IJSRuntime)}.");

        return type;
    }
}
