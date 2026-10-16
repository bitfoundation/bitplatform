using System.Text.RegularExpressions;
using Bunit;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Components;

[TestClass]
public class BelemetryComponentsTests
{
    private const string ModulePath = "./_content/Bit.Belemetry/bit-belemetry.js";

    private static BunitContext CreateContext(Action<BelemetryOptions>? configure = null)
    {
        var context = new BunitContext();
        context.Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
        context.Services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            configure?.Invoke(o);
        });
        return context;
    }

    private static string ConfigJson(string markup)
    {
        var match = Regex.Match(markup, "<script type=\"application/json\" id=\"bit-belemetry-config\"[^>]*>(.*?)</script>", RegexOptions.Singleline);
        Assert.IsTrue(match.Success, markup);
        return match.Groups[1].Value;
    }

    [TestMethod]
    public async Task Script_RendersTheConfigAndTheModule()
    {
        await using var context = CreateContext();

        var markup = context.Render<BelemetryScript>().Markup;

        using var config = JsonDocument.Parse(ConfigJson(markup));
        Assert.AreEqual("belemetry-tests", config.RootElement.GetProperty("service").GetProperty("name").GetString());
        Assert.AreEqual(4095, config.RootElement.GetProperty("features").GetInt32());
        StringAssert.Contains(markup, "<script type=\"module\" src=\"_content/Bit.Belemetry/bit-belemetry.js\"></script>");
        Assert.DoesNotContain("nonce", markup);
    }

    [TestMethod]
    public async Task Script_AppliesTheNonceAndSrc_Encoded()
    {
        await using var context = CreateContext();

        var markup = context.Render<BelemetryScript>(p => p
            .Add(c => c.Nonce, "abc\"def")
            .Add(c => c.Src, "_content/Bit.Belemetry/bit-belemetry.123.js?v=1&x=\"y\"")).Markup;

        Assert.AreEqual(2, Regex.Matches(markup, "nonce=\"abc&quot;def\"").Count, markup);
        StringAssert.Contains(markup, "src=\"_content/Bit.Belemetry/bit-belemetry.123.js?v=1&amp;x=&quot;y&quot;\"");
    }

    [TestMethod]
    public async Task Script_EscapesScriptInjectionInConfigValues()
    {
        const string attack = "</script><script>alert(1)</script><!--";
        await using var context = CreateContext(o =>
        {
            o.ServiceName = attack;
            o.ResourceAttributes["note"] = "a & b <c>";
            o.Browser.IgnoreErrorMessages.Add(attack);
        });

        var markup = context.Render<BelemetryScript>().Markup;

        Assert.AreEqual(2, Regex.Matches(markup, "</script>", RegexOptions.IgnoreCase).Count, "Only the two real script elements close.");
        Assert.DoesNotContain("<script>alert", markup);
        Assert.DoesNotContain("<!--", markup);

        using var config = JsonDocument.Parse(ConfigJson(markup));
        Assert.AreEqual(attack, config.RootElement.GetProperty("service").GetProperty("name").GetString(), "The value survives intact once parsed.");
        Assert.AreEqual("a & b <c>", config.RootElement.GetProperty("resource").GetProperty("note").GetString());
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, false)]
    public async Task Script_RendersNothingWhenDisabled(bool enabled, bool browserEnabled)
    {
        await using var context = CreateContext(o =>
        {
            o.Enabled = enabled;
            o.Browser.Enabled = browserEnabled;
        });

        Assert.AreEqual(string.Empty, context.Render<BelemetryScript>().Markup.Trim());
    }

    [TestMethod]
    public async Task Initializer_ImportsAndStarts_InStrictMode()
    {
        await using var context = CreateContext(o => o.Browser.Transport = BelemetryBrowserTransport.Relay);
        context.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = context.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("start", _ => true).SetVoidResult();
        // The attach options are a JsonDocument the bridge disposes once the call is made, so read them in the matcher.
        string? attachOptions = null;
        module.SetupVoid("attachDotNet", invocation =>
        {
            attachOptions = ((JsonElement)invocation.Arguments[1]!).GetRawText();
            return true;
        }).SetVoidResult();
        using var contextJson = JsonDocument.Parse("""{"consent":3,"decided":true,"sessionId":"s-1","pseudoId":null,"userId":null}""");
        module.Setup<JsonElement>("getContext").SetResult(contextJson.RootElement.Clone());
        var ready = 0;

        context.Render<BelemetryInitializer>(p => p.Add(c => c.OnReady, () => ready++));

        Assert.AreEqual(1, ready);
        Assert.AreEqual(ModulePath, context.JSInterop.VerifyInvoke("import").Arguments.Single());

        var start = module.VerifyInvoke("start");
        var config = (JsonElement)start.Arguments.Single()!;
        Assert.AreEqual("belemetry-tests", config.GetProperty("service").GetProperty("name").GetString());
        Assert.AreEqual("relay", config.GetProperty("transport").GetString());

        Assert.IsInstanceOfType<Microsoft.JSInterop.DotNetObjectReference<BelemetryJsCallbacks>>(module.VerifyInvoke("attachDotNet").Arguments[0]);
        Assert.AreEqual("""{"relay":true,"unloadDrain":false}""", attachOptions);

        // The browser's decision is adopted by .NET.
        var consent = context.Services.GetRequiredService<BelemetryConsentState>();
        Assert.IsTrue(consent.IsDecided);
        Assert.AreEqual(BelemetryFeatures.Errors | BelemetryFeatures.PageViews, consent.Granted);
        Assert.AreEqual("s-1", consent.SessionId);
    }

    [TestMethod]
    public async Task Initializer_StartsInLooseMode_AndStartIsIdempotent()
    {
        await using var context = CreateContext(o => o.Browser.Transport = BelemetryBrowserTransport.Direct);
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = context.JSInterop.SetupModule(ModulePath);

        context.Render<BelemetryInitializer>();
        await context.Services.GetRequiredService<BelemetryJsBridge>().StartAsync();

        Assert.HasCount(1, module.Invocations["start"]);
        Assert.HasCount(1, module.Invocations["attachDotNet"]);
        Assert.AreEqual("direct", ((JsonElement)module.Invocations["start"][0].Arguments[0]!).GetProperty("transport").GetString());
    }

    [TestMethod]
    public async Task Initializer_DoesNothingWhenTheBrowserIsDisabled()
    {
        await using var context = CreateContext(o => o.Browser.Enabled = false);
        context.JSInterop.Mode = JSRuntimeMode.Strict;
        var ready = false;

        context.Render<BelemetryInitializer>(p => p.Add(c => c.OnReady, () => ready = true));

        Assert.IsFalse(ready);
        Assert.IsEmpty(context.JSInterop.Invocations);
    }

    [TestMethod]
    public async Task Initializer_ToleratesAScriptThatFailsToLoad()
    {
        await using var context = CreateContext();
        context.Services.AddSingleton<Microsoft.JSInterop.IJSRuntime, FailingJsRuntime>();
        var ready = false;

        context.Render<BelemetryInitializer>(p => p.Add(c => c.OnReady, () => ready = true));

        Assert.IsFalse(ready, "Telemetry never breaks the page.");
    }

    private sealed class FailingJsRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new Microsoft.JSInterop.JSException("Failed to fetch dynamically imported module");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }
}
