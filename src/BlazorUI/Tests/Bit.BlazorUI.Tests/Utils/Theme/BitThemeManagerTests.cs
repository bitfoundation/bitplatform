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
}
