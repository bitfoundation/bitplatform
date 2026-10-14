using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Bonboarding.Tests.Infrastructure;

/// <summary>
/// Spins up the real services against <see cref="FakeInterop"/>. Tests exercise the shipping
/// engine, not a simplified copy of it.
/// </summary>
internal sealed class BonboardingHarness : IAsyncDisposable
{
    private readonly ServiceProvider _root;
    private readonly AsyncServiceScope _scope;

    public BonboardingHarness(Action<BonboardingOptions>? configure = null, bool withNavigation = true)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        services.AddBitBonboardingServices(configure);

        Interop = new FakeInterop();
        services.AddSingleton<IBonboardingInterop>(Interop);
        services.AddBonboardingInMemoryStore();

        if (withNavigation)
        {
            Navigation = new TestNavigationManager();
            services.AddSingleton<NavigationManager>(Navigation);
        }

        Events = new RecordingObserver();
        services.AddSingleton<IBonboardingObserver>(Events);

        _root = services.BuildServiceProvider();
        _scope = _root.CreateAsyncScope();

        Service = _scope.ServiceProvider.GetRequiredService<IBonboardingService>();
    }

    public FakeInterop Interop { get; }

    public TestNavigationManager? Navigation { get; }

    public RecordingObserver Events { get; }

    public IBonboardingService Service { get; }

    public IServiceProvider Services => _scope.ServiceProvider;

    public IBonboardingStore Store => _scope.ServiceProvider.GetRequiredService<IBonboardingStore>();

    /// <summary>Registers a selector as present in the fake document.</summary>
    public BonboardingHarness WithElements(params string[] selectors)
    {
        foreach (var selector in selectors) Interop.ExistingSelectors.Add(selector);
        return this;
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _root.DisposeAsync();
    }
}

/// <summary>A navigation manager that records navigations instead of touching a browser.</summary>
internal sealed class TestNavigationManager : NavigationManager
{
    public TestNavigationManager(string baseUri = "https://localhost/", string uri = "https://localhost/")
        => Initialize(baseUri, uri);

    public List<string> Navigations { get; } = [];

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Navigations.Add(uri);
        Uri = ToAbsoluteUri(uri).ToString();
        NotifyLocationChanged(false);
    }

    /// <summary>Simulates the user navigating, for example by clicking a link.</summary>
    public void SimulateUserNavigation(string relativeUri)
    {
        Uri = ToAbsoluteUri(relativeUri).ToString();
        NotifyLocationChanged(true);
    }
}

/// <summary>Collects every event the service publishes, for assertions on analytics behaviour.</summary>
internal sealed class RecordingObserver : IBonboardingObserver
{
    public List<BonboardingEvent> Events { get; } = [];

    public IReadOnlyList<BonboardingEventKind> Kinds => Events.Select(e => e.Kind).ToArray();

    public ValueTask OnEventAsync(BonboardingEvent onboardingEvent, CancellationToken cancellationToken = default)
    {
        Events.Add(onboardingEvent);
        return ValueTask.CompletedTask;
    }
}
