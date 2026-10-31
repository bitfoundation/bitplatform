using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// Where the components take their services from. An app that lazy loads Bit.Bonboarding on WebAssembly cannot call
/// AddBitBonboardingServices, so the components create their own and GetBonboardingService hands out the same
/// service; an app that did register keeps exactly what it registered.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BunitContext
{
    public LazyLoadingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Bonboarding/bit-bonboarding.js");
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<BonboardingBrowserEnvironment>("initialize", _ => true).SetResult(new BonboardingBrowserEnvironment());
        module.Setup<BonboardingStepGeometry>("activateStep", _ => true).SetResult(BonboardingStepGeometry.Missing);
    }

    private static BonboardingTourDefinition Tour(string id, string title = "Welcome") => new()
    {
        Id = id,
        Title = title,
        Persist = false,
        Steps = [new BonboardingStepDefinition { Id = $"{id}-1", Title = $"{title}, step one" }],
    };

    [TestMethod]
    public async Task An_app_that_registered_bonboarding_keeps_its_own_services()
    {
        await using var app = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(new PrerenderJSRuntime()).AddBitBonboardingServices().BuildServiceProvider();
        await using var scope = app.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var runtime = BonboardingRuntime.Of(services);

        Assert.AreSame(services.GetRequiredService<IBonboardingService>(), runtime.Service);
        Assert.AreSame(services.GetRequiredService<BonboardingGuidanceRegistry>(), runtime.Guidance);
        Assert.AreSame(services.GetRequiredService<BonboardingHostRegistry>(), runtime.Hosts);
        Assert.AreSame(services.GetRequiredService<IBonboardingInterop>(), runtime.Interop);
        Assert.AreSame(services.GetRequiredService<IBonboardingLocalizer>(), runtime.Localizer);
        Assert.AreSame(services.GetRequiredService<IBonboardingService>(), services.GetBonboardingService());
    }

    [TestMethod]
    public async Task A_registered_IBonboardingService_is_handed_out_even_without_the_rest_of_the_registration()
    {
        // A test double or a decorator: only the interface is registered.
        await using var harness = new BonboardingHarness();
        Services.AddSingleton(harness.Service);

        Assert.AreSame(harness.Service, Services.GetBonboardingService());
    }

    [TestMethod]
    public async Task Without_the_registration_bonboarding_creates_its_services_once_per_browser_connection()
    {
        var jsRuntime = new PrerenderJSRuntime();
        await using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        await using var otherConnection = new ServiceCollection().AddSingleton<IJSRuntime>(new PrerenderJSRuntime()).BuildServiceProvider();

        var runtime = BonboardingRuntime.Of(first);

        Assert.AreSame(runtime, BonboardingRuntime.Of(second));
        Assert.AreSame(runtime.Service, first.GetBonboardingService());
        Assert.AreSame(first.GetBonboardingService(), second.GetBonboardingService());
        Assert.AreNotSame(runtime, BonboardingRuntime.Of(otherConnection));
        Assert.AreNotSame(first.GetBonboardingService(), otherConnection.GetBonboardingService());
    }

    [TestMethod]
    public async Task A_partial_registration_keeps_its_options_observers_and_services_but_never_mixes_in_its_own()
    {
        var observer = new RecordingObserver();
        var marker = new object();
        var collection = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new PrerenderJSRuntime())
            .Configure<BonboardingOptions>(options => options.Labels.Next = "Onward")
            .AddSingleton<IBonboardingObserver>(observer)
            .AddKeyedSingleton("marker", marker);
        await using var app = collection.BuildServiceProvider();

        var runtime = BonboardingRuntime.Of(app);

        Assert.AreEqual("Onward", runtime.Localizer.GetLabels(null).Next);

        // A tour's hooks reach the application's own services through their context, not Bonboarding's.
        object? seen = null;
        var tour = Tour("partial");
        tour.CanStart = context =>
        {
            seen = context.Services.GetKeyedService<object>("marker");
            return ValueTask.FromResult(true);
        };

        await runtime.Service.StartAsync(tour);

        Assert.AreSame(marker, seen);
        Assert.Contains(BonboardingEventKind.TourStarted, observer.Kinds);
    }

    [TestMethod]
    public async Task Without_the_registration_bonboarding_warns_once_per_app_rather_than_once_per_connection()
    {
        var warnings = new List<string>();
        var loggers = new SinkLoggerFactory(warnings);
        await using var firstCircuit = new ServiceCollection().AddSingleton<IJSRuntime>(new PrerenderJSRuntime()).AddSingleton<ILoggerFactory>(loggers).BuildServiceProvider();
        await using var secondCircuit = new ServiceCollection().AddSingleton<IJSRuntime>(new PrerenderJSRuntime()).AddSingleton<ILoggerFactory>(loggers).BuildServiceProvider();

        firstCircuit.GetBonboardingService();
        firstCircuit.GetBonboardingService();
        secondCircuit.GetBonboardingService();

        var warning = warnings.Single();
        Assert.Contains("AddBitBonboardingServices", warning);
    }

    [TestMethod]
    public async Task Without_the_registration_or_a_Blazor_app_the_error_names_the_registration()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetBonboardingService());

        Assert.Contains("AddBitBonboardingServices", error.Message);
    }

    [TestMethod]
    public void GetBonboardingService_rejects_null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitBonboarding.GetBonboardingService(null!));
    }

    [TestMethod]
    public async Task The_components_work_without_the_registration()
    {
        var root = Render<BonboardingRoot>();
        Render<BonboardingTour>(parameters => parameters
            .Add(tour => tour.Id, "markup")
            .Add(tour => tour.Title, "Declared in markup")
            .AddChildContent<BonboardingStep>(step => step.Add(s => s.Title, "From the markup")));
        var list = Render<BonboardingTourList>();

        // What app code reaches through GetBonboardingService is the service the components work with.
        var service = Services.GetBonboardingService();
        Assert.IsNotNull(service.GetTour("markup"));
        list.WaitForAssertion(() => Assert.Contains("Declared in markup", list.Markup));

        await root.InvokeAsync(() => service.StartAsync(Tour("welcome")));

        root.WaitForAssertion(() => Assert.AreEqual("Welcome, step one", root.Find(".bit-bonboarding-title").TextContent));
        Assert.AreEqual("welcome", service.Active?.Tour.Id);
    }

    [TestMethod]
    public async Task A_component_that_failed_to_start_does_not_fail_again_when_disposed()
    {
        // Half a registration: the host registry is there, the rest is not, so resolving the services throws.
        Services.AddScoped<BonboardingHostRegistry>();

        Assert.ThrowsExactly<InvalidOperationException>(() => Render<BonboardingRoot>());
        Assert.ThrowsExactly<InvalidOperationException>(() => Render<BonboardingTourList>());

        // Disposing them must not resolve the services again, and throw a second time.
        await DisposeComponentsAsync();
        Assert.IsNull(Renderer.UnhandledException.IsCompleted ? await Renderer.UnhandledException : null);
    }

    /// <summary>A JS runtime as it is while prerendering: every call fails the way Blazor's does then.</summary>
    private sealed class PrerenderJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");
    }

    private sealed class SinkLoggerFactory(List<string> warnings) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => new SinkLogger(warnings);
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class SinkLogger(List<string> warnings) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) warnings.Add(formatter(state, exception));
        }
    }
}
