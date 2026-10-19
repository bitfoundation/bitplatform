using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

[TestClass]
public class ServiceGraphTests
{
    public interface IRepo { }

    public sealed class Repo : IRepo { }

    public sealed class ScopedThing(IRepo repo)
    {
        public IRepo Repo { get; } = repo;
    }

    public sealed class Captor(ScopedThing thing)
    {
        public ScopedThing Thing { get; } = thing;
    }

    public sealed class A(B b)
    {
        public B B { get; } = b;
    }

    public sealed class B(A a)
    {
        public A A { get; } = a;
    }

    [TestMethod]
    public void Builds_dependencies_and_consumers_from_constructors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepo, Repo>();
        services.AddScoped<ScopedThing>();
        var graph = BevServiceGraph.Build(services);

        var thing = graph.Services.Single(s => s.ServiceType == typeof(ScopedThing));
        var repo = graph.Services.Single(s => s.ServiceType == typeof(IRepo));
        Assert.ContainsSingle(d => d == repo, thing.Dependencies);
        Assert.ContainsSingle(c => c == thing, repo.Consumers);
        Assert.AreEqual(ServiceLifetime.Scoped, thing.Lifetime);
        Assert.AreEqual("Repo", repo.ImplementationName);
        Assert.AreEqual(1, thing.SubtreeSize);
    }

    [TestMethod]
    public void Captive_dependency_is_reported_as_error()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepo, Repo>();
        services.AddScoped<ScopedThing>();
        services.AddSingleton<Captor>();
        var findings = BevServiceGraph.Build(services).Analyze().ToList();

        var captive = Assert.ContainsSingle(f => f.RuleId == "di.captive", findings);
        Assert.Contains("Captor captures scoped ScopedThing", captive.Title);
        Assert.AreEqual(BevDiagnosticSeverity.Error, captive.Severity);
    }

    [TestMethod]
    public void Circular_dependencies_are_detected_once()
    {
        var services = new ServiceCollection();
        services.AddScoped<A>();
        services.AddScoped<B>();
        var graph = BevServiceGraph.Build(services);

        var cycle = Assert.ContainsSingle(graph.FindCycles());
        Assert.AreEqual(2, cycle.Count);
        var finding = Assert.ContainsSingle(f => f.RuleId == "di.cycle", graph.Analyze());
        Assert.Contains("A → B → A", finding.Title);
    }

    [TestMethod]
    public void Shadowed_registrations_are_flagged_and_unresolved_dependencies_listed()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedThing>(); // IRepo not registered
        services.AddSingleton<IRepo, Repo>();
        services.AddSingleton<IRepo>(new Repo());
        var graph = BevServiceGraph.Build(services);

        var repos = graph.Services.Where(s => s.ServiceType == typeof(IRepo)).ToList();
        Assert.IsTrue(repos[0].IsShadowed);
        Assert.IsFalse(repos[1].IsShadowed);
        Assert.AreEqual("Instance", repos[1].RegistrationKind);
        Assert.IsEmpty(graph.Services.Single(s => s.ServiceType == typeof(ScopedThing)).UnresolvedDependencies);
    }

    [TestMethod]
    public void Registry_captures_the_collection_passed_to_AddBitBevToolsServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedThing>();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        services.AddSingleton<IRepo, Repo>(); // registered after: still visible because the collection is enumerated lazily
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<BevToolsRegistry>();
        Assert.IsNotNull(registry.ServiceGraph);
        Assert.Contains(s => s.ServiceType == typeof(IRepo), registry.ServiceGraph!.Services);
    }
}
