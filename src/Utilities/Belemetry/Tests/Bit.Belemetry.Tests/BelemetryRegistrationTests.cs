using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bit.Belemetry.Tests;

[TestClass]
public class BelemetryRegistrationTests
{
    [TestMethod]
    public async Task AddBitBelemetryServices_IsIdempotent_AndRunsEveryConfigureCallbackInOrder()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.ServiceName = "first";
            o.Environment = "Staging";
        });
        var count = services.Count;
        services.AddBitBelemetryServices(o => o.ServiceName = "second");
        services.AddBitBelemetryServices();

        // Only the options configuration grows; every Belemetry service is registered once.
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(BelemetryPipeline)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(BelemetrySettings)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(BelemetryHostDetection)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(BelemetryConsentState)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IBelemetry)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IBelemetryConsent)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(BelemetryJsBridge)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ILoggerProvider) && d.ImplementationType == typeof(BelemetryLoggerProvider)));
        Assert.AreEqual(1, services.Count - count, "Only the second configure callback was added.");

        await using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<BelemetrySettings>();
        Assert.AreEqual("second", settings.ServiceName, "Callbacks run in registration order.");
        Assert.AreEqual("Staging", settings.Options.Environment, "Earlier callbacks are not lost.");
        Assert.AreSame(settings.Options, provider.GetRequiredService<IOptions<BelemetryOptions>>().Value);
        Assert.HasCount(1, provider.GetServices<ILoggerProvider>().OfType<BelemetryLoggerProvider>());
    }

    [TestMethod]
    public void AddBitBelemetryServices_ThrowsOnNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Belemetry.AddBitBelemetryServices(null!));
    }

    [TestMethod]
    public async Task ScopedServices_ResolveFromAPlainContainer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.JSInterop.IJSRuntime, NoJsRuntime>();
        services.AddBitBelemetryServices(TestHost.ApplyTestDefaults);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IBelemetry>());
        Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IBelemetryConsent>());
    }

    [TestMethod]
    public async Task AHostWithoutBlazor_HasNoJsRuntime_AndStillGetsIBelemetry()
    {
        // An API or a worker registers no IJSRuntime: the bridge has nothing to start and the calls go to the pipeline.
        var memory = new BelemetryInMemoryExporter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Consent.Default = BelemetryFeatures.All;
            o.AddExporter(memory);
        });
        await using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<ILoggerFactory>();
        await using var scope = provider.CreateAsyncScope();

        var belemetry = scope.ServiceProvider.GetRequiredService<IBelemetry>();
        belemetry.TrackEvent("worker.started");
        await belemetry.FlushAsync();

        Assert.AreEqual("worker.started", memory.Logs.Single().EventName);
    }

    [TestMethod]
    public async Task Consent_NeverGrantsMoreThanIsAvailable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.JSInterop.IJSRuntime, NoJsRuntime>();
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Features = BelemetryFeatures.Errors | BelemetryFeatures.Logs;
            o.Consent.Default = BelemetryFeatures.All;
        });
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var consent = scope.ServiceProvider.GetRequiredService<IBelemetryConsent>();
        var available = BelemetryFeatures.Errors | BelemetryFeatures.Logs;

        Assert.AreEqual(available, consent.Available);
        Assert.AreEqual(available, consent.Granted, "The default is narrowed to what is configured.");

        await consent.SetAsync(BelemetryFeatures.All);
        Assert.AreEqual(available, consent.Granted);
        Assert.AreEqual(available, provider.GetRequiredService<BelemetryConsentState>().Granted, "Only the available part is recorded.");
        Assert.IsFalse(consent.IsGranted(BelemetryFeatures.PageViews));

        await consent.GrantAsync(BelemetryFeatures.Metrics);
        Assert.AreEqual(available, consent.Granted);
    }

    private sealed class NoJsRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new InvalidOperationException("No JS while prerendering.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new InvalidOperationException("No JS while prerendering.");
    }
}
