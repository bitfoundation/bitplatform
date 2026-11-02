using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Balette.Tests;

/// <summary>
/// Where the components take their services from. An app that lazy loads Bit.Balette on WebAssembly cannot call
/// AddBitBaletteServices, so the components create their own and GetBaletteService hands out the same registry; an app
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

        var runtime = BaletteRuntime.Of(app);

        Assert.AreSame(app.GetRequiredService<BaletteService>(), runtime.Service);
        Assert.AreSame(app.GetRequiredService<BaletteOptions>(), runtime.Options);
        Assert.AreSame(app.GetRequiredService<BaletteHistory>(), runtime.History);
        Assert.AreSame(app.GetRequiredService<IBaletteService>(), app.GetBaletteService());
    }

    [TestMethod]
    public void A_registered_IBaletteService_is_used_even_without_a_BaletteService_behind_it()
    {
        // A test double or a decorator: only the interface is registered.
        var registered = new BaletteService(new BaletteOptions());
        Services.AddSingleton<IBaletteService>(registered);

        Render<BaletteCommands>(p => p.Add(x => x.Commands, [BaletteCmd.Navigate("Settings", "/settings").Id("settings")]));

        Assert.AreSame(registered, Services.GetBaletteService());
        Assert.IsNotNull(registered.Find("settings"));
    }

    [TestMethod]
    public void Without_the_registration_balette_creates_its_services_once_per_browser_connection()
    {
        var jsRuntime = new NoJSRuntime();
        using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var otherConnection = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).BuildServiceProvider();

        var runtime = BaletteRuntime.Of(first);

        Assert.AreSame(runtime, BaletteRuntime.Of(second));
        Assert.AreSame(runtime.Service, first.GetBaletteService());
        Assert.AreSame(first.GetBaletteService(), second.GetBaletteService());
        Assert.AreNotSame(runtime, BaletteRuntime.Of(otherConnection));
        Assert.AreNotSame(first.GetBaletteService(), otherConnection.GetBaletteService());
    }

    [TestMethod]
    public async Task A_partial_registration_keeps_its_options_and_logging_but_never_mixes_its_services_with_the_defaults()
    {
        var options = new BaletteOptions { EnableHistory = false };
        var collection = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddSingleton(options);
        var logs = LogSink.AddTo(collection);
        await using var app = collection.BuildServiceProvider();

        Assert.AreSame(options, BaletteRuntime.Of(app).Options);

        // The registry's warnings still reach the application's log.
        var palette = app.GetBaletteService();
        palette.Register(BaletteCmd.New("Home").Id("home"), BaletteCmd.New("Start").Id("home"));
        Assert.IsTrue(logs.Messages(LogLevel.Warning).Any(message => message.Contains("home", StringComparison.Ordinal)), string.Join(Environment.NewLine, logs.Entries));
    }

    [TestMethod]
    public async Task Without_the_registration_balette_warns_once_per_app_rather_than_once_per_connection()
    {
        var logs = new LogSink();
        var loggers = new SinkLoggerFactory(logs);
        await using var firstCircuit = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddSingleton<ILoggerFactory>(loggers).BuildServiceProvider();
        await using var secondCircuit = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddSingleton<ILoggerFactory>(loggers).BuildServiceProvider();

        firstCircuit.GetBaletteService();
        firstCircuit.GetBaletteService();
        secondCircuit.GetBaletteService();

        var warning = logs.Messages(LogLevel.Warning).Single();
        StringAssert.Contains(warning, "AddBitBaletteServices");
    }

    [TestMethod]
    public async Task Without_the_registration_or_a_Blazor_app_the_error_names_the_registration()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBaletteService());

        StringAssert.Contains(error.Message, "AddBitBaletteServices");
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

    [TestMethod]
    public async Task The_trigger_shows_and_announces_what_the_palette_it_opens_answers_to()
    {
        // Rendered before the palette, as in a layout's header: it catches up once the palette is there.
        var trigger = Render<BaletteTrigger>();
        var palette = Render<Balette>(p => p
            .Add(x => x.ToggleShortcut, "Mod+P")
            .Add(x => x.Strings, new BaletteStrings { OpenPalette = "فرمان‌ها", Search = "جستجو" }));

        trigger.WaitForAssertion(() =>
        {
            var button = trigger.Find("button");
            Assert.AreEqual("Control+P", button.GetAttribute("aria-keyshortcuts"));
            Assert.AreEqual("فرمان‌ها", button.GetAttribute("aria-label"));
            Assert.AreEqual("جستجو", trigger.Find(".bit-balette-trigger-label").TextContent);
        });

        // Without the palette, the trigger goes back to the options.
        await palette.Instance.DisposeAsync();
        trigger.WaitForAssertion(() => Assert.AreEqual("Control+K", trigger.Find("button").GetAttribute("aria-keyshortcuts")));
    }

    [TestMethod]
    public async Task A_palette_that_failed_to_start_does_not_fail_again_when_disposed()
    {
        // Half a registration: the BaletteService is there, the rest is not, so resolving the services throws.
        Services.AddSingleton(new BaletteService(new BaletteOptions()));

        Assert.ThrowsExactly<InvalidOperationException>(() => Render<Balette>());
        Assert.ThrowsExactly<InvalidOperationException>(() => Render<BaletteTrigger>());

        // Disposing them must not resolve the services again, and throw a second time.
        await DisposeComponentsAsync();
        Assert.IsNull(Renderer.UnhandledException.IsCompleted ? await Renderer.UnhandledException : null);
    }

    private sealed class SinkLoggerFactory(LogSink sink) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => new SinkLogger<object>(sink);
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    }
}
