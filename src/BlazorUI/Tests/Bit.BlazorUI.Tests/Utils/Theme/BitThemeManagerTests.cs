using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeManagerTests : BunitTestContext
{
    [TestMethod]
    public async Task SetThemeAsyncNormalizesNameToCanonicalLowercaseForm()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        await manager.SetThemeAsync("  Fluent-Light  ");

        // The raw string API must normalize the same way BitThemeName does, so the value written
        // onto bit-theme matches the lowercase :root[bit-theme=...] selectors in the packaged CSS.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.set");
        Assert.AreEqual("fluent-light", invocation.Arguments[0]);
    }

    [TestMethod]
    public async Task SetThemeAsyncWithBitThemeNameWritesNormalizedValue()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        await manager.SetThemeAsync(BitThemeName.Custom("Brand-DARK"));

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.set");
        Assert.AreEqual("brand-dark", invocation.Arguments[0]);
    }

    [TestMethod]
    public async Task SetThemeAsyncBlankNameIsForwardedAsEmptyNoOp()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        await manager.SetThemeAsync("   ");

        // Whitespace-only collapses to empty string, which the client script treats as a no-op
        // (keeps the current theme) rather than writing a literal whitespace attribute value.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.set");
        Assert.AreEqual(string.Empty, invocation.Arguments[0]);
    }

    [TestMethod]
    public async Task SetThemeAsyncLeavesAlreadyCanonicalNameUnchanged()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        await manager.SetThemeAsync(BitThemePresets.FluentDark);

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.set");
        Assert.AreEqual("fluent-dark", invocation.Arguments[0]);
    }

    [TestMethod]
    public async Task SetThemePairAsyncNormalizesBothNames()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        await manager.SetThemePairAsync("  Material-Light ", "MATERIAL-dark");

        // The pair lands on bit-theme-light / bit-theme-dark and from there on bit-theme, so it is
        // normalized exactly as SetThemeAsync normalizes a single name.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.setPair");
        Assert.AreEqual("material-light", invocation.Arguments[0]);
        Assert.AreEqual("material-dark", invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task SetThemePairAsyncWithBitThemeNamesWritesTheirValues()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        await manager.SetThemePairAsync(BitThemeName.Custom("Brand-Light"), BitThemeName.Custom("Brand-Dark"));

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.setPair");
        Assert.AreEqual("brand-light", invocation.Arguments[0]);
        Assert.AreEqual("brand-dark", invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task SetThemePairAsyncForwardsABlankNameAsTheEmptyNoOp()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        await manager.SetThemePairAsync("fluent2-light", "   ");

        // The client script ignores a pair with a blank half rather than storing half a pair.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.setPair");
        Assert.AreEqual("fluent2-light", invocation.Arguments[0]);
        Assert.AreEqual(string.Empty, invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task NotifierIsRegisteredExactlyOnceAcrossManagerCalls()
    {
        var manager = CreateFullyWiredManager();

        await manager.SetThemeAsync("dark");
        await manager.ToggleDarkLightAsync();
        await manager.EnsureThemeNotificationsRegisteredAsync();

        // Every public method funnels through the same idempotent registration step, so only the
        // first call may reach the JS side.
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.registerDotNetNotifier", calledTimes: 1);
    }

    [TestMethod]
    public async Task DisposeUnregistersNotifierExactlyOnce()
    {
        var manager = CreateFullyWiredManager();
        await manager.EnsureThemeNotificationsRegisteredAsync();

        await manager.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.unregisterDotNetNotifier", calledTimes: 1);
    }

    [TestMethod]
    public async Task DoubleDisposeIsSafeAndDoesNotDuplicateUnregistration()
    {
        var manager = CreateFullyWiredManager();
        await manager.EnsureThemeNotificationsRegisteredAsync();

        await manager.DisposeAsync();
        await manager.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.unregisterDotNetNotifier", calledTimes: 1);
    }

    [TestMethod]
    public async Task PublicMethodsThrowAfterDispose()
    {
        var manager = CreateFullyWiredManager();
        await manager.EnsureThemeNotificationsRegisteredAsync();

        await manager.DisposeAsync();

        // Every public method funnels through the registration step, which fails fast on a disposed
        // manager instead of reaching JS interop with a torn-down DotNetObjectReference.
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await manager.GetCurrentThemeAsync());
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await manager.SetThemePairAsync("light", "dark"));
    }

    /// <summary>
    /// Resolves a manager through DI so it is built via the internal receiver-wired constructor
    /// (the ctor is not visible to this assembly, but the DI factory registered by
    /// AddBitBlazorUIServices uses it) - the receiver-less public ctor never registers a notifier.
    /// </summary>
    private BitThemeManager CreateFullyWiredManager()
    {
        Services.AddBitBlazorUIServices();
        return Services.GetRequiredService<BitThemeManager>();
    }
}
