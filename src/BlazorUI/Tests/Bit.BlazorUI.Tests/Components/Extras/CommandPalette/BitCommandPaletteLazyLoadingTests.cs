using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// Where the components take their services from. An app that lazy loads Bit.BlazorUI.Extras on WebAssembly cannot call
/// AddBitBlazorUIExtrasServices, so the components create their own and GetBitCommandPaletteService hands out the same registry; an app
/// that did register keeps exactly what it registered.
/// </summary>
[TestClass]
public sealed class BitCommandPaletteLazyLoadingTests : BunitTestContext
{
    [TestInitialize]
    public void Initialize()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var handle = Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        handle.Setup<bool>("isMac").SetResult(false);
    }

    [TestMethod]
    public async Task An_app_that_registered_the_command_palette_keeps_its_own_services()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddBitBlazorUIExtrasServices().BuildServiceProvider();

        var runtime = BitCommandPaletteRuntime.Of(app);

        Assert.AreSame(app.GetRequiredService<BitCommandPaletteService>(), runtime.Service);
        Assert.AreSame(app.GetRequiredService<BitCommandPaletteOptions>(), runtime.Options);
        Assert.AreSame(app.GetRequiredService<BitCommandPaletteHistory>(), runtime.History);
        Assert.AreSame(app.GetRequiredService<IBitCommandPaletteService>(), app.GetBitCommandPaletteService());
    }

    [TestMethod]
    public void A_registered_IBitCommandPaletteService_is_used_even_without_a_BitCommandPaletteService_behind_it()
    {
        // A test double or a decorator: only the interface is registered.
        var registered = new BitCommandPaletteService(new BitCommandPaletteOptions());
        Services.AddSingleton<IBitCommandPaletteService>(registered);

        Context.Render<BitCommandPaletteCommands>(p => p.Add(x => x.Commands, [BitCommandPaletteCmd.Navigate("Settings", "/settings").Id("settings")]));

        Assert.AreSame(registered, Services.GetBitCommandPaletteService());
        Assert.IsNotNull(registered.Find("settings"));
    }

    [TestMethod]
    public void Without_the_registration_the_command_palette_creates_its_services_once_per_browser_connection()
    {
        var jsRuntime = new NoJSRuntime();
        using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var otherConnection = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).BuildServiceProvider();

        var runtime = BitCommandPaletteRuntime.Of(first);

        Assert.AreSame(runtime, BitCommandPaletteRuntime.Of(second));
        Assert.AreSame(runtime.Service, first.GetBitCommandPaletteService());
        Assert.AreSame(first.GetBitCommandPaletteService(), second.GetBitCommandPaletteService());
        Assert.AreNotSame(runtime, BitCommandPaletteRuntime.Of(otherConnection));
        Assert.AreNotSame(first.GetBitCommandPaletteService(), otherConnection.GetBitCommandPaletteService());
    }

    [TestMethod]
    public async Task A_partial_registration_keeps_its_options_and_logging_but_never_mixes_its_services_with_the_defaults()
    {
        var options = new BitCommandPaletteOptions { EnableHistory = false };
        var collection = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddSingleton(options);
        var logs = LogSink.AddTo(collection);
        await using var app = collection.BuildServiceProvider();

        Assert.AreSame(options, BitCommandPaletteRuntime.Of(app).Options);

        // The registry's warnings still reach the application's log.
        var palette = app.GetBitCommandPaletteService();
        palette.Register(BitCommandPaletteCmd.New("Home").Id("home"), BitCommandPaletteCmd.New("Start").Id("home"));
        Assert.IsTrue(logs.Messages(LogLevel.Warning).Any(message => message.Contains("home", StringComparison.Ordinal)), string.Join(Environment.NewLine, logs.Entries));
    }

    [TestMethod]
    public async Task Without_the_registration_the_command_palette_warns_once_per_app_rather_than_once_per_connection()
    {
        var logs = new LogSink();
        var loggers = new SinkLoggerFactory(logs);
        await using var firstCircuit = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddSingleton<ILoggerFactory>(loggers).BuildServiceProvider();
        await using var secondCircuit = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddSingleton<ILoggerFactory>(loggers).BuildServiceProvider();

        firstCircuit.GetBitCommandPaletteService();
        firstCircuit.GetBitCommandPaletteService();
        secondCircuit.GetBitCommandPaletteService();

        var warning = logs.Messages(LogLevel.Warning).Single();
        StringAssert.Contains(warning, "AddBitBlazorUIExtrasServices");
    }

    [TestMethod]
    public async Task Without_the_registration_or_a_Blazor_app_the_error_names_the_registration()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBitCommandPaletteService());

        StringAssert.Contains(error.Message, "AddBitBlazorUIExtrasServices");
    }

    [TestMethod]
    public async Task The_palette_its_trigger_and_its_commands_work_without_the_registration()
    {
        Context.Render<BitCommandPaletteCommands>(p => p.Add(x => x.Commands, [BitCommandPaletteCmd.Navigate("Settings", "/settings").Id("settings").Group("Navigation")]));
        var palette = Context.Render<BitCommandPalette>();
        var trigger = Context.Render<BitCommandPaletteTrigger>();

        // What app code reaches through GetBitCommandPaletteService is the registry the palette lists.
        Services.GetBitCommandPaletteService().Register(BitCommandPaletteCmd.Action("Toggle sidebar", () => { }).Id("sidebar").Group("View"));

        Assert.AreEqual("Control+K", trigger.Find("button").GetAttribute("aria-keyshortcuts"));
        await trigger.InvokeAsync(() => trigger.Find("button").Click());

        palette.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", palette.Find("dialog").GetAttribute("data-open"));
            AssertSequence.AreEqual(["settings", "sidebar"], palette.FindAll("[data-bit-cpl=item]").Select(item => item.GetAttribute("data-value")));
        });
        Assert.IsTrue(Services.GetBitCommandPaletteService().IsOpen);
    }

    [TestMethod]
    public async Task The_trigger_shows_and_announces_what_the_palette_it_opens_answers_to()
    {
        // Rendered before the palette, as in a layout's header: it catches up once the palette is there.
        var trigger = Context.Render<BitCommandPaletteTrigger>();
        var palette = Context.Render<BitCommandPalette>(p => p
            .Add(x => x.ToggleShortcut, "Mod+P")
            .Add(x => x.Strings, new BitCommandPaletteStrings { OpenPalette = "فرمان‌ها", Search = "جستجو", TriggerNameSeparator = "، " }));

        trigger.WaitForAssertion(() =>
        {
            var button = trigger.Find("button");
            Assert.AreEqual("Control+P", button.GetAttribute("aria-keyshortcuts"));
            Assert.AreEqual("جستجو، فرمان‌ها", button.GetAttribute("aria-label"));
            Assert.AreEqual("جستجو", trigger.Find(".bit-cpt-lbl").TextContent);
        });

        // Without the palette, the trigger goes back to the options.
        await palette.Instance.DisposeAsync();
        trigger.WaitForAssertion(() => Assert.AreEqual("Control+K", trigger.Find("button").GetAttribute("aria-keyshortcuts")));
    }

    [TestMethod]
    public async Task A_palette_that_failed_to_start_does_not_fail_again_when_disposed()
    {
        // Half a registration: the BitCommandPaletteService is there, the rest is not, so resolving the services throws.
        Services.AddSingleton(new BitCommandPaletteService(new BitCommandPaletteOptions()));

        Assert.ThrowsExactly<InvalidOperationException>(() => Context.Render<BitCommandPalette>());
        Assert.ThrowsExactly<InvalidOperationException>(() => Context.Render<BitCommandPaletteTrigger>());

        // Disposing them must not resolve the services again, and throw a second time.
        await Context.DisposeComponentsAsync();
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
