using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// Where the components take their services from. An app that lazy loads Bit.Bonboarding on WebAssembly cannot call
/// AddBitBonboardingServices, so it turns the Bit.Bonboarding.LazyLoad switch on, the components create their own and
/// GetBonboardingService hands out the same service; an app that did register keeps exactly what it registered, and one
/// that did neither fails the way it always has.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BunitContext
{
    public LazyLoadingTests()
    {
        // What a lazy loading app's .csproj turns on. The switch is process-wide, so every test turns it off again.
        AppContext.SetSwitch(BonboardingRuntime.LazyLoadSwitch, true);

        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Bonboarding/bit-bonboarding.js");
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<BonboardingBrowserEnvironment>("initialize", _ => true).SetResult(new BonboardingBrowserEnvironment());
        module.Setup<BonboardingStepGeometry>("activateStep", _ => true).SetResult(BonboardingStepGeometry.Missing);
    }

    [TestCleanup]
    public void TurnTheSwitchOff() => AppContext.SetSwitch(BonboardingRuntime.LazyLoadSwitch, false);

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

        // One for the scope, rather than one for every component in it.
        Assert.AreSame(runtime, BonboardingRuntime.Of(services));
    }

    [TestMethod]
    public async Task A_registered_IBonboardingService_is_the_one_both_the_components_and_app_code_use()
    {
        // A test double or a hand-wired service: only the interface and the clock are registered, which is all a
        // BonboardingTour needs.
        await using var harness = new BonboardingHarness();
        Services.AddSingleton(harness.Service);
        Services.AddSingleton(TimeProvider.System);

        Render<BonboardingTour>(parameters => parameters
            .Add(tour => tour.Id, "markup")
            .AddChildContent<BonboardingStep>(step => step.Add(s => s.Title, "From the markup")));

        Assert.IsNotNull(harness.Service.GetTour("markup"), "The tour in the markup registered with the registered service.");
        Assert.AreSame(harness.Service, Services.GetBonboardingService());
    }

    [TestMethod]
    public void Without_the_registration_or_the_switch_bonboarding_fails_naming_both()
    {
        AppContext.SetSwitch(BonboardingRuntime.LazyLoadSwitch, false);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => Services.GetBonboardingService());

        Assert.Contains("AddBitBonboardingServices", error.Message);
        Assert.Contains(BonboardingRuntime.LazyLoadSwitch, error.Message);
        Assert.ThrowsExactly<InvalidOperationException>(() => Render<BonboardingRoot>());
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
    public async Task Without_the_registration_the_options_observers_and_services_the_app_registered_are_honoured()
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
    public async Task Without_the_registration_a_store_and_a_localizer_the_app_registered_replace_the_defaults()
    {
        var store = new BonboardingInMemoryStore();
        var localizer = new FixedLocalizer();
        await using var app = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new PrerenderJSRuntime())
            .AddSingleton<IBonboardingStore>(store)
            .AddSingleton<IBonboardingLocalizer>(localizer)
            .BuildServiceProvider();

        var runtime = BonboardingRuntime.Of(app);
        await store.SetAsync(new BonboardingRecord { TourId = "kept" });

        Assert.AreSame(localizer, runtime.Localizer);
        Assert.IsNotNull(await runtime.Service.GetRecordAsync("kept"), "The record was read from the registered store, not the browser.");
    }

    [TestMethod]
    public async Task A_scope_app_code_asked_from_first_gives_way_to_the_one_the_components_are_given()
    {
        // As on WebAssembly: one IJSRuntime for the whole app, the renderer's scope for the components, and a shorter one
        // an OwningComponentBase's ScopedServices would be.
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new PrerenderJSRuntime()).AddScoped<ScopedMarker>().BuildServiceProvider();
        await using var components = app.CreateAsyncScope();
        var owned = app.CreateAsyncScope();

        var service = owned.ServiceProvider.GetBonboardingService();
        new BonboardingRuntime.Handle(typeof(BonboardingRoot), BonboardingRuntime.Needs.Service).Get(components.ServiceProvider);
        await owned.DisposeAsync();

        ScopedMarker? seen = null;
        var tour = Tour("scoped");
        tour.CanStart = context =>
        {
            seen = context.Services.GetRequiredService<ScopedMarker>();
            return ValueTask.FromResult(true);
        };

        await service.StartAsync(tour);

        Assert.AreSame(components.ServiceProvider.GetRequiredService<ScopedMarker>(), seen);
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
        // The service alone is registered: the host and the list need more of the registration, so resolving fails.
        await using var harness = new BonboardingHarness();
        Services.AddSingleton(harness.Service);

        Assert.ThrowsExactly<InvalidOperationException>(() => Render<BonboardingRoot>());
        Assert.ThrowsExactly<InvalidOperationException>(() => Render<BonboardingTourList>());

        // Disposing them must not resolve the services again, and throw a second time.
        await DisposeComponentsAsync();
        Assert.IsNull(Renderer.UnhandledException.IsCompleted ? await Renderer.UnhandledException : null);
    }

    [TestMethod]
    public async Task A_failed_resolution_is_not_tried_again()
    {
        var attempts = 0;
        await using var services = new ServiceCollection()
            .AddScoped<IBonboardingService>(_ => throw new InvalidOperationException($"Attempt {++attempts}."))
            .BuildServiceProvider();
        var handle = new BonboardingRuntime.Handle(typeof(BonboardingTour), BonboardingRuntime.Needs.Service);

        Assert.ThrowsExactly<InvalidOperationException>(() => handle.Get(services));
        var again = Assert.ThrowsExactly<InvalidOperationException>(() => handle.Get(services));

        Assert.AreEqual("Attempt 1.", again.Message);
        Assert.AreEqual(1, attempts);
    }

    [TestMethod]
    public void A_disposed_component_resolves_nothing_more()
    {
        var unused = new BonboardingRuntime.Handle(typeof(BonboardingTour), BonboardingRuntime.Needs.Service);

        Assert.IsNull(unused.Close());
        var error = Assert.ThrowsExactly<ObjectDisposedException>(() => unused.Get(Services));
        Assert.Contains(nameof(BonboardingTour), error.ObjectName);

        // What it did resolve stays, for the members that still read it on the way out.
        var used = new BonboardingRuntime.Handle(typeof(BonboardingTour), BonboardingRuntime.Needs.Service);
        var runtime = used.Get(Services);

        Assert.AreSame(runtime, used.Close());
        Assert.AreSame(runtime, used.Get(Services));
    }

    /// <summary>A JS runtime as it is while prerendering: every call fails the way Blazor's does then.</summary>
    private sealed class PrerenderJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");
    }

    private sealed class FixedLocalizer : IBonboardingLocalizer
    {
        public BonboardingLabels GetLabels(BonboardingTourDefinition? tour) => new();
    }

    private sealed class ScopedMarker;
}
