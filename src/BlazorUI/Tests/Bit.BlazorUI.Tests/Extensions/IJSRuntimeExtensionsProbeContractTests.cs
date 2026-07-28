using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions;

/// <summary>
/// <see cref="IJSRuntimeExtensions.IsRuntimeInvalid"/> probes framework-internal type names and
/// members reflectively and deliberately degrades to "runtime is valid" when they are missing -
/// safe at runtime, but a .NET upgrade that reshapes those internals would silently disable the
/// prerender/disconnect guards. These tests run once per target framework of this project and pin
/// each probed member, so such an upgrade fails loudly here instead.
/// </summary>
[TestClass]
public sealed class IJSRuntimeExtensionsProbeContractTests
{
    [TestMethod]
    public void RemoteJsRuntimeStillExposesIsInitialized()
    {
        var type = FindTypeByName("Microsoft.AspNetCore.Components.Server", "RemoteJSRuntime");
        Assert.IsNotNull(type, "RemoteJSRuntime no longer exists under that name; the Blazor Server probe in IsRuntimeInvalid is dead.");

        var property = type.GetProperty("IsInitialized", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(property, "RemoteJSRuntime.IsInitialized is gone; the Blazor Server probe in IsRuntimeInvalid silently reports every runtime as valid.");
        Assert.AreEqual(typeof(bool), property.PropertyType, "RemoteJSRuntime.IsInitialized changed type; the probe's `is false` check no longer matches.");
    }

    [TestMethod]
    public void WebViewJsRuntimeStillExposesIpcSenderField()
    {
        var type = FindTypeByName("Microsoft.AspNetCore.Components.WebView", "WebViewJSRuntime");
        Assert.IsNotNull(type, "WebViewJSRuntime no longer exists under that name; the Blazor Hybrid probe in IsRuntimeInvalid is dead.");

        var field = type.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "WebViewJSRuntime._ipcSender is gone; the Blazor Hybrid probe in IsRuntimeInvalid silently reports every runtime as valid.");
    }

    [TestMethod]
    public void UnsupportedJavaScriptRuntimeTypeNameStillExists()
    {
        // The prerendering probe matches by simple type name only, and the framework has shipped the
        // type from more than one assembly over time - finding it in either keeps the contract alive.
        var type = FindTypeByName("Microsoft.AspNetCore.Components.Endpoints", "UnsupportedJavaScriptRuntime")
                   ?? FindTypeByName("Microsoft.AspNetCore.Mvc.ViewFeatures", "UnsupportedJavaScriptRuntime");

        Assert.IsNotNull(type, "No framework assembly defines UnsupportedJavaScriptRuntime anymore; prerendering interop is no longer short-circuited by IsRuntimeInvalid.");
    }

    [TestMethod]
    public void UnsupportedJavaScriptRuntimeInstanceIsReportedInvalid()
    {
        var type = FindTypeByName("Microsoft.AspNetCore.Components.Endpoints", "UnsupportedJavaScriptRuntime")
                   ?? FindTypeByName("Microsoft.AspNetCore.Mvc.ViewFeatures", "UnsupportedJavaScriptRuntime");
        Assert.IsNotNull(type);

        // End-to-end through the real probe: a live instance of the prerendering runtime must be
        // reported invalid so library interop calls no-op instead of throwing during prerender.
        var instance = (IJSRuntime)Activator.CreateInstance(type, nonPublic: true)!;
        Assert.IsTrue(instance.IsRuntimeInvalid());
    }

    [TestMethod]
    public void UnknownRuntimeTypesAreTreatedAsValid()
    {
        // The documented fail-safe: anything the probe does not recognize (WASM, test doubles,
        // custom runtimes) must be treated as valid so interop proceeds and surfaces real errors.
        Assert.IsFalse(new FakeJsRuntime().IsRuntimeInvalid());
    }

    private static Type? FindTypeByName(string assemblyName, string typeName)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(assemblyName);
        }
        catch (Exception)
        {
            return null; // assembly itself is gone; callers assert on the resulting null.
        }

        return assembly.DefinedTypes.FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.Ordinal));
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }
}
