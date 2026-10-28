using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Balette.Tests;

/// <summary>
/// Where the components take their services from. An app that lazy loads Bit.Balette on WebAssembly cannot call
/// AddBitBaletteServices, so the components build their own and GetBaletteService hands out the same registry; an app
/// that did register keeps exactly what it registered.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BunitContext
{
    public LazyLoadingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        var handle = module.SetupModule("init", _ => true);
        handle.Setup<bool>("isMac").SetResult(false);
    }

    [TestMethod]
    public async Task An_app_that_registered_balette_keeps_its_own_services()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddBitBaletteServices().BuildServiceProvider();

        Assert.AreSame(app, BitBalette.GetServices(app));
        Assert.AreSame(app.GetRequiredService<IBaletteService>(), app.GetBaletteService());
    }

    [TestMethod]
    public void Without_the_registration_balette_builds_its_services_once_per_browser_connection()
    {
        var jsRuntime = new NoJSRuntime();
        using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var otherConnection = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).BuildServiceProvider();

        var services = BitBalette.GetServices(first);

        Assert.IsNotNull(services.GetService<BaletteHistory>());
        Assert.IsInstanceOfType<BaletteBrowserStorageHistoryStore>(services.GetRequiredService<IBaletteHistoryStore>());
        Assert.AreSame(services.GetRequiredService<BaletteService>(), services.GetRequiredService<IBaletteService>());
        Assert.AreSame(services, BitBalette.GetServices(second));
        Assert.AreSame(first.GetBaletteService(), second.GetBaletteService());
        Assert.AreNotSame(services, BitBalette.GetServices(otherConnection));
        Assert.AreNotSame(first.GetBaletteService(), otherConnection.GetBaletteService());
    }

    [TestMethod]
    public async Task A_partial_registration_keeps_its_options_and_logging_but_never_mixes_its_services_with_the_defaults()
    {
        var options = new BaletteOptions { EnableHistory = false };
        var logs = new LogSink();
        await using var app = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new NoJSRuntime())
            .AddSingleton(options)
            .AddSingleton<ILoggerFactory>(new SinkLoggerFactory(logs))
            .BuildServiceProvider();

        var services = BitBalette.GetServices(app);

        Assert.AreNotSame(app, services);
        Assert.AreSame(options, services.GetRequiredService<BaletteOptions>());

        // The registry's warnings still reach the application's log.
        var palette = app.GetBaletteService();
        palette.Register(BaletteCmd.New("Home").Id("home"), BaletteCmd.New("Start").Id("home"));
        Assert.IsTrue(logs.Messages(LogLevel.Warning).Any(message => message.Contains("home", StringComparison.Ordinal)), string.Join(Environment.NewLine, logs.Entries));
    }

    [TestMethod]
    public async Task The_palette_its_trigger_and_its_commands_work_without_the_registration()
    {
        Render<BaletteCommands>(p => p.Add(x => x.Commands, [BaletteCmd.Navigate("Settings", "/settings").Id("settings").Group("Navigation")]));
        var palette = Render<Balette>();
        var trigger = Render<BaletteTrigger>();

        // What app code reaches through GetBaletteService is the registry the palette lists.
        Services.GetBaletteService().Register(BaletteCmd.Action("Toggle sidebar", () => { }).Id("sidebar").Group("View"));

        Assert.AreEqual("Control+K", trigger.Find("button").GetAttribute("aria-keyshortcuts"));
        await trigger.InvokeAsync(() => trigger.Find("button").Click());

        palette.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", palette.Find("dialog").GetAttribute("data-open"));
            AssertSequence.AreEqual(["settings", "sidebar"], palette.FindAll("[data-balette=item]").Select(item => item.GetAttribute("data-value")));
        });
        Assert.IsTrue(Services.GetBaletteService().IsOpen);
    }

    private sealed class SinkLoggerFactory(LogSink sink) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => new SinkLogger<object>(sink);
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }
}
